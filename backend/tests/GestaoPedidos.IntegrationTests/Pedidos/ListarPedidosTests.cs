using System.Net;
using GestaoPedidos.IntegrationTests.Support;
using Shouldly;

namespace GestaoPedidos.IntegrationTests.Pedidos;

public sealed class ListarPedidosTests(SqlServerFixture sql) : TesteDeIntegracao(sql)
{
    private int _produtoId;
    private HttpClient? _api;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _produtoId = await new ProdutoBuilder().ComPreco(10m).ComEstoque(1000).InserirAsync(Banco);
    }

    private static DateTime Utc(int mes, int dia, int hora = 12, int minuto = 0, int segundo = 0, int milissegundo = 0)
    {
        return new DateTime(2026, mes, dia, hora, minuto, segundo, milissegundo, DateTimeKind.Utc);
    }

    private Task<int> ClienteAsync(string nome = "Cliente de Teste")
    {
        return new ClienteBuilder().ComNome(nome).InserirAsync(Banco);
    }

    private Task<int> PedidoAsync(int clienteId, DateTime dataUtc, decimal preco = 10m)
    {
        return new PedidoBuilder(clienteId).ComItem(_produtoId, 1, preco).InserirEmAsync(Banco, dataUtc);
    }

    private HttpClient Api => _api ??= CriarCliente();

    private async Task<PaginaLida> ListarAsync(string consulta = "")
    {
        var resposta = await Api.ListarPedidosAsync(consulta);
        return await resposta.DeveSerPaginaAsync();
    }

    [Fact]
    public async Task Lists_orders_newest_first_with_id_desc_as_tiebreaker()
    {
        var ana = await ClienteAsync("Ana");
        var bruno = await ClienteAsync("Bruno");
        var p1 = await PedidoAsync(ana, Utc(3, 1, 10));
        var p2 = await PedidoAsync(bruno, Utc(3, 5, 8));
        var p3 = await PedidoAsync(ana, Utc(3, 5, 8));
        var p4 = await PedidoAsync(bruno, Utc(2, 20));
        var p5 = await PedidoAsync(ana, Utc(3, 5, 8, 0, 0, 1));

        var pagina = await ListarAsync();

        pagina.Ids.ShouldBe([p5, p3, p2, p1, p4]);
        pagina.TotalItems.ShouldBe(5);
        pagina.TotalPages.ShouldBe(1);
    }

    [Fact]
    public async Task Returns_the_summary_fields_of_each_order()
    {
        var clienteId = await ClienteAsync("Maria Resumo");
        var outroProduto = await new ProdutoBuilder().ComPreco(7.25m).InserirAsync(Banco);
        var data = Utc(3, 10, 14, 30, 15, 250);
        var pedidoId = await new PedidoBuilder(clienteId)
            .ComItem(_produtoId, 1, 12.50m)
            .ComItem(outroProduto, 1, 7.25m)
            .InserirEmAsync(Banco, data);

        var pagina = await ListarAsync();

        pagina.Items.ShouldBe([new ResumoLido(pedidoId, clienteId, "Maria Resumo", data, 19.75m, 2)]);
    }

    [Fact]
    public async Task Uses_page_1_and_page_size_20_by_default()
    {
        var clienteId = await ClienteAsync();
        for (var i = 0; i < 25; i++)
        {
            await PedidoAsync(clienteId, Utc(3, 1).AddMinutes(i));
        }

        var pagina = await ListarAsync();

        pagina.Page.ShouldBe(1);
        pagina.PageSize.ShouldBe(20);
        pagina.Items.Count.ShouldBe(20);
        pagina.TotalItems.ShouldBe(25);
        pagina.TotalPages.ShouldBe(2);
    }

    [Fact]
    public async Task Filters_by_customer()
    {
        var ana = await ClienteAsync("Ana");
        var bruno = await ClienteAsync("Bruno");
        var a1 = await PedidoAsync(ana, Utc(3, 1));
        await PedidoAsync(bruno, Utc(3, 2));
        var a2 = await PedidoAsync(ana, Utc(3, 3));
        await PedidoAsync(bruno, Utc(3, 4));

        var pagina = await ListarAsync($"clienteId={ana}");

        pagina.Ids.ShouldBe([a2, a1]);
        pagina.Items.ShouldAllBe(i => i.ClienteId == ana && i.ClienteNome == "Ana");
        pagina.TotalItems.ShouldBe(2);
        pagina.TotalPages.ShouldBe(1);
    }

    [Fact]
    public async Task Includes_orders_at_both_ends_of_the_period()
    {
        var clienteId = await ClienteAsync();
        await PedidoAsync(clienteId, Utc(3, 9, 23, 59, 59, 999));
        var inicio = await PedidoAsync(clienteId, Utc(3, 10, 0, 0, 0, 0));
        var meio = await PedidoAsync(clienteId, Utc(3, 12, 12));
        var fim = await PedidoAsync(clienteId, Utc(3, 15, 23, 59, 59, 999));
        await PedidoAsync(clienteId, Utc(3, 16, 0, 0, 0, 0));

        var pagina = await ListarAsync("dataInicio=2026-03-10&dataFim=2026-03-15");

        pagina.Ids.ShouldBe([fim, meio, inicio]);
        pagina.TotalItems.ShouldBe(3);
    }

    [Fact]
    public async Task Filters_with_only_the_start_date()
    {
        var clienteId = await ClienteAsync();
        await PedidoAsync(clienteId, Utc(3, 9, 23, 59, 59, 999));
        var inicio = await PedidoAsync(clienteId, Utc(3, 10, 0, 0, 0, 0));
        var depois = await PedidoAsync(clienteId, Utc(4, 20));

        var pagina = await ListarAsync("dataInicio=2026-03-10");

        pagina.Ids.ShouldBe([depois, inicio]);
        pagina.TotalItems.ShouldBe(2);
    }

    [Fact]
    public async Task Filters_with_only_the_end_date()
    {
        var clienteId = await ClienteAsync();
        var antes = await PedidoAsync(clienteId, Utc(1, 5));
        var fim = await PedidoAsync(clienteId, Utc(3, 15, 23, 59, 59, 999));
        await PedidoAsync(clienteId, Utc(3, 16, 0, 0, 0, 0));

        var pagina = await ListarAsync("dataFim=2026-03-15");

        pagina.Ids.ShouldBe([fim, antes]);
        pagina.TotalItems.ShouldBe(2);
    }

    [Fact]
    public async Task Same_start_and_end_date_returns_the_whole_day()
    {
        var clienteId = await ClienteAsync();
        await PedidoAsync(clienteId, Utc(3, 9, 23, 59, 59, 999));
        var madrugada = await PedidoAsync(clienteId, Utc(3, 10, 0, 0, 0, 0));
        var tarde = await PedidoAsync(clienteId, Utc(3, 10, 15));
        var noite = await PedidoAsync(clienteId, Utc(3, 10, 23, 59, 59, 999));
        await PedidoAsync(clienteId, Utc(3, 11, 0, 0, 0, 0));

        var pagina = await ListarAsync("dataInicio=2026-03-10&dataFim=2026-03-10");

        pagina.Ids.ShouldBe([noite, tarde, madrugada]);
        pagina.TotalItems.ShouldBe(3);
    }

    [Fact]
    public async Task Combines_customer_and_period_filters()
    {
        var ana = await ClienteAsync("Ana");
        var bruno = await ClienteAsync("Bruno");
        await PedidoAsync(ana, Utc(3, 1));
        var dentro = await PedidoAsync(ana, Utc(3, 10));
        await PedidoAsync(bruno, Utc(3, 10));
        await PedidoAsync(ana, Utc(3, 20));

        var pagina = await ListarAsync($"clienteId={ana}&dataInicio=2026-03-05&dataFim=2026-03-15");

        pagina.Ids.ShouldBe([dentro]);
        pagina.TotalItems.ShouldBe(1);
        pagina.TotalPages.ShouldBe(1);
    }

    [Fact]
    public async Task Pages_through_all_orders_without_repeating_or_skipping()
    {
        var clienteId = await ClienteAsync();
        var criados = new List<(int Id, DateTime Data)>();
        for (var i = 0; i < 45; i++)
        {
            var data = Utc(3, 1).AddHours(i / 3);
            criados.Add((await PedidoAsync(clienteId, data), data));
        }

        var esperado = criados.OrderByDescending(c => c.Data).ThenByDescending(c => c.Id).Select(c => c.Id).ToList();
        var lidos = new List<int>();
        for (var page = 1; page <= 5; page++)
        {
            var pagina = await ListarAsync($"page={page}&pageSize=10");
            pagina.Page.ShouldBe(page);
            pagina.PageSize.ShouldBe(10);
            pagina.TotalItems.ShouldBe(45);
            pagina.TotalPages.ShouldBe(5);
            pagina.Items.Count.ShouldBe(page < 5 ? 10 : 5);
            lidos.AddRange(pagina.Ids);
        }

        lidos.ShouldBe(esperado);
    }

    [Fact]
    public async Task Returns_an_empty_page_beyond_the_last_with_correct_totals()
    {
        var clienteId = await ClienteAsync();
        await PedidoAsync(clienteId, Utc(3, 1));
        await PedidoAsync(clienteId, Utc(3, 2));
        var maisAntigo = await PedidoAsync(clienteId, Utc(2, 1));

        var ultima = await ListarAsync("page=2&pageSize=2");
        var alem = await ListarAsync("page=5&pageSize=2");

        ultima.Ids.ShouldBe([maisAntigo]);
        alem.Items.ShouldBeEmpty();
        alem.Page.ShouldBe(5);
        alem.PageSize.ShouldBe(2);
        alem.TotalItems.ShouldBe(3);
        alem.TotalPages.ShouldBe(2);
    }

    [Theory]
    [InlineData(1, 21)]
    [InlineData(7, 3)]
    [InlineData(20, 2)]
    [InlineData(21, 1)]
    [InlineData(100, 1)]
    public async Task Computes_total_pages_as_the_ceiling_of_total_items_by_page_size(int pageSize, int totalPages)
    {
        var clienteId = await ClienteAsync();
        for (var i = 0; i < 21; i++)
        {
            await PedidoAsync(clienteId, Utc(3, 1).AddMinutes(i));
        }

        var pagina = await ListarAsync($"pageSize={pageSize}");

        pagina.TotalItems.ShouldBe(21);
        pagina.TotalPages.ShouldBe(totalPages);
        pagina.PageSize.ShouldBe(pageSize);
        pagina.Items.Count.ShouldBe(Math.Min(pageSize, 21));
    }

    [Fact]
    public async Task Returns_zero_totals_when_there_are_no_orders()
    {
        var pagina = await ListarAsync();

        pagina.Items.ShouldBeEmpty();
        pagina.Page.ShouldBe(1);
        pagina.PageSize.ShouldBe(20);
        pagina.TotalItems.ShouldBe(0);
        pagina.TotalPages.ShouldBe(0);
    }

    [Fact]
    public async Task Returns_zero_totals_when_no_order_matches_the_filters()
    {
        var comPedidos = await ClienteAsync("Com Pedidos");
        var semPedidos = await ClienteAsync("Sem Pedidos");
        await PedidoAsync(comPedidos, Utc(3, 1));

        var porCliente = await ListarAsync($"clienteId={semPedidos}");
        var porPeriodo = await ListarAsync("dataInicio=2025-01-01&dataFim=2025-01-31");

        foreach (var pagina in new[] { porCliente, porPeriodo })
        {
            pagina.Items.ShouldBeEmpty();
            pagina.TotalItems.ShouldBe(0);
            pagina.TotalPages.ShouldBe(0);
        }
    }

    [Theory]
    [InlineData("page=0", "page", "pagina_invalida")]
    [InlineData("page=-1", "page", "pagina_invalida")]
    [InlineData("page=abc", "page", "pagina_invalida")]
    [InlineData("page=1.5", "page", "pagina_invalida")]
    [InlineData("pageSize=0", "pageSize", "tamanho_pagina_invalido")]
    [InlineData("pageSize=-5", "pageSize", "tamanho_pagina_invalido")]
    [InlineData("pageSize=101", "pageSize", "tamanho_pagina_invalido")]
    [InlineData("pageSize=abc", "pageSize", "tamanho_pagina_invalido")]
    [InlineData("clienteId=0", "clienteId", "cliente_id_invalido")]
    [InlineData("clienteId=-3", "clienteId", "cliente_id_invalido")]
    [InlineData("clienteId=abc", "clienteId", "cliente_id_invalido")]
    [InlineData("dataInicio=2026-13-01", "dataInicio", "data_invalida")]
    [InlineData("dataInicio=2026-02-30", "dataInicio", "data_invalida")]
    [InlineData("dataInicio=15/03/2026", "dataInicio", "data_invalida")]
    [InlineData("dataInicio=2026-3-5", "dataInicio", "data_invalida")]
    [InlineData("dataInicio=2026-03-15T00:00:00", "dataInicio", "data_invalida")]
    [InlineData("dataInicio=abc", "dataInicio", "data_invalida")]
    [InlineData("dataFim=2026-13-01", "dataFim", "data_invalida")]
    [InlineData("dataFim=2026-02-30", "dataFim", "data_invalida")]
    [InlineData("dataFim=15/03/2026", "dataFim", "data_invalida")]
    [InlineData("dataFim=abc", "dataFim", "data_invalida")]
    [InlineData("dataInicio=2026-03-11&dataFim=2026-03-10", "dataInicio", "periodo_invertido")]
    public async Task Rejects_invalid_query_parameters_with_the_field_code(string consulta, string campo, string code)
    {
        var resposta = await Api.ListarPedidosAsync(consulta);

        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.BadRequest, "validacao_falhou");
        var erros = problema.ErrosDeCampo();
        erros.Select(e => (e.Campo, e.Code)).ShouldBe([(campo, code)]);
        erros.ShouldAllBe(e => !string.IsNullOrWhiteSpace(e.Mensagem));
    }

    [Fact]
    public async Task Reports_every_out_of_range_parameter_at_once()
    {
        var resposta = await Api.ListarPedidosAsync("page=0&pageSize=101&clienteId=0&dataInicio=2026-03-11&dataFim=2026-03-10");

        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.BadRequest, "validacao_falhou");
        problema.ErrosDeCampo().Select(e => (e.Campo, e.Code)).ShouldBe(
            [
                ("page", "pagina_invalida"),
                ("pageSize", "tamanho_pagina_invalido"),
                ("clienteId", "cliente_id_invalido"),
                ("dataInicio", "periodo_invertido"),
            ],
            ignoreOrder: true);
    }

    [Fact]
    public async Task Reports_every_unparseable_parameter_at_once()
    {
        var resposta = await Api.ListarPedidosAsync("page=abc&pageSize=xyz&clienteId=abc&dataInicio=abc&dataFim=2026-02-30");

        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.BadRequest, "validacao_falhou");
        problema.ErrosDeCampo().Select(e => (e.Campo, e.Code)).ShouldBe(
            [
                ("page", "pagina_invalida"),
                ("pageSize", "tamanho_pagina_invalido"),
                ("clienteId", "cliente_id_invalido"),
                ("dataInicio", "data_invalida"),
                ("dataFim", "data_invalida"),
            ],
            ignoreOrder: true);
    }

    [Fact]
    public async Task Reports_unparseable_and_out_of_range_parameters_together()
    {
        var resposta = await Api.ListarPedidosAsync("page=abc&pageSize=101&clienteId=0&dataInicio=xyz");

        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.BadRequest, "validacao_falhou");
        problema.ErrosDeCampo().Select(e => (e.Campo, e.Code)).ShouldBe(
            [
                ("page", "pagina_invalida"),
                ("pageSize", "tamanho_pagina_invalido"),
                ("clienteId", "cliente_id_invalido"),
                ("dataInicio", "data_invalida"),
            ],
            ignoreOrder: true);
    }
}
