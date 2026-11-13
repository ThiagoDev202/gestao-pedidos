using System.Net;
using GestaoPedidos.IntegrationTests.Support;
using Shouldly;

namespace GestaoPedidos.IntegrationTests.Relatorios;

public sealed class TopClientesTests(SqlServerFixture sql) : TesteDeIntegracao(sql)
{
    private const string Marco = "dataInicio=2026-03-01&dataFim=2026-03-31";

    private int _produtoId;
    private HttpClient? _api;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _produtoId = await new ProdutoBuilder().ComPreco(1m).ComEstoque(1000).InserirAsync(Banco);
    }

    private HttpClient Api => _api ??= CriarCliente();

    private static DateTime Utc(int mes, int dia, int hora = 12, int minuto = 0, int segundo = 0, int milissegundo = 0)
    {
        return new DateTime(2026, mes, dia, hora, minuto, segundo, milissegundo, DateTimeKind.Utc);
    }

    private Task<int> ClienteAsync(string nome)
    {
        return new ClienteBuilder().ComNome(nome).InserirAsync(Banco);
    }

    private Task<int> PedidoAsync(int clienteId, decimal valor, DateTime? dataUtc = null)
    {
        return new PedidoBuilder(clienteId).ComItem(_produtoId, 1, valor).InserirEmAsync(Banco, dataUtc ?? Utc(3, 15));
    }

    private async Task<IReadOnlyList<TopClienteLido>> RelatorioAsync(string consulta = Marco)
    {
        var resposta = await Api.TopClientesAsync(consulta);
        return await resposta.DeveSerRankingAsync();
    }

    [Fact]
    public async Task Returns_exactly_five_customers_ordered_by_total_desc()
    {
        var c1 = await ClienteAsync("Cliente Um");
        var c2 = await ClienteAsync("Cliente Dois");
        var c3 = await ClienteAsync("Cliente Tres");
        var c4 = await ClienteAsync("Cliente Quatro");
        var c5 = await ClienteAsync("Cliente Cinco");
        var c6 = await ClienteAsync("Cliente Seis");
        var c7 = await ClienteAsync("Cliente Sete");
        await PedidoAsync(c1, 200m, Utc(3, 2));
        await PedidoAsync(c1, 300m, Utc(3, 20));
        await PedidoAsync(c2, 300m);
        await PedidoAsync(c3, 100m, Utc(3, 3));
        await PedidoAsync(c3, 250m, Utc(3, 4));
        await PedidoAsync(c3, 350m, Utc(3, 30));
        await PedidoAsync(c4, 100m);
        await PedidoAsync(c5, 400m);
        await PedidoAsync(c6, 150m, Utc(3, 6));
        await PedidoAsync(c6, 50m, Utc(3, 7));
        await PedidoAsync(c7, 50m);

        var ranking = await RelatorioAsync();

        ranking.ShouldBe(
            [
                new TopClienteLido(c3, "Cliente Tres", 700m, 3),
                new TopClienteLido(c1, "Cliente Um", 500m, 2),
                new TopClienteLido(c5, "Cliente Cinco", 400m, 1),
                new TopClienteLido(c2, "Cliente Dois", 300m, 1),
                new TopClienteLido(c6, "Cliente Seis", 200m, 2),
            ]);
    }

    [Fact]
    public async Task Sums_the_order_totals_of_each_customer_and_counts_its_orders()
    {
        var clienteId = await ClienteAsync("Somadora");
        var outroProduto = await new ProdutoBuilder().ComPreco(2.5m).InserirAsync(Banco);
        await new PedidoBuilder(clienteId).ComItem(_produtoId, 3, 10.10m).ComItem(outroProduto, 2, 2.50m).InserirEmAsync(Banco, Utc(3, 5));
        await PedidoAsync(clienteId, 19.99m, Utc(3, 6));
        await PedidoAsync(clienteId, 0.01m, Utc(3, 7));

        var ranking = await RelatorioAsync();

        ranking.ShouldBe([new TopClienteLido(clienteId, "Somadora", 55.30m, 3)]);
    }

    [Fact]
    public async Task Lists_namesake_customers_as_separate_entries()
    {
        var joao1 = await ClienteAsync("João Silva");
        var ana = await ClienteAsync("Ana Souza");
        var joao2 = await ClienteAsync("João Silva");
        await PedidoAsync(joao1, 60m);
        await PedidoAsync(joao1, 40m);
        await PedidoAsync(ana, 90m);
        await PedidoAsync(joao2, 80m);

        var ranking = await RelatorioAsync();

        ranking.ShouldBe(
            [
                new TopClienteLido(joao1, "João Silva", 100m, 2),
                new TopClienteLido(ana, "Ana Souza", 90m, 1),
                new TopClienteLido(joao2, "João Silva", 80m, 1),
            ]);
    }

    [Fact]
    public async Task Breaks_ties_by_name_then_by_customer_id()
    {
        var carla = await ClienteAsync("Carla");
        var bruno1 = await ClienteAsync("Bruno");
        var ana = await ClienteAsync("Ana");
        var bruno2 = await ClienteAsync("Bruno");
        var maior = await ClienteAsync("Zeca");
        await PedidoAsync(maior, 75m);
        foreach (var clienteId in new[] { carla, bruno1, ana, bruno2 })
        {
            await PedidoAsync(clienteId, 50m);
        }

        var ranking = await RelatorioAsync();

        ranking.Select(r => r.ClienteId).ShouldBe([maior, ana, bruno1, bruno2, carla]);
    }

    [Fact]
    public async Task Uses_the_name_to_decide_who_enters_the_top_five_on_a_tie()
    {
        var nomes = new[] { "Fabio", "Elisa", "Diego", "Carla", "Bruno", "Ana" };
        var ids = new Dictionary<string, int>();
        foreach (var nome in nomes)
        {
            ids[nome] = await ClienteAsync(nome);
            await PedidoAsync(ids[nome], 50m);
        }

        var ranking = await RelatorioAsync();

        ranking.Select(r => r.Nome).ShouldBe(["Ana", "Bruno", "Carla", "Diego", "Elisa"]);
        ranking.ShouldAllBe(r => r.ValorTotal == 50m && r.QuantidadePedidos == 1);
        ranking.Select(r => r.ClienteId).ShouldNotContain(ids["Fabio"]);
    }

    [Fact]
    public async Task Leaves_out_customers_without_orders_in_the_period()
    {
        var noPeriodo = await ClienteAsync("No Periodo");
        var foraDoPeriodo = await ClienteAsync("Fora Do Periodo");
        await ClienteAsync("Sem Pedidos");
        await PedidoAsync(noPeriodo, 10m);
        await PedidoAsync(foraDoPeriodo, 999m, Utc(2, 28));
        await PedidoAsync(foraDoPeriodo, 999m, Utc(4, 1));

        var ranking = await RelatorioAsync();

        ranking.ShouldBe([new TopClienteLido(noPeriodo, "No Periodo", 10m, 1)]);
    }

    [Fact]
    public async Task Counts_only_the_orders_inside_the_period_for_each_customer()
    {
        var clienteId = await ClienteAsync("Parcial");
        await PedidoAsync(clienteId, 30m, Utc(3, 10));
        await PedidoAsync(clienteId, 500m, Utc(2, 10));
        await PedidoAsync(clienteId, 700m, Utc(4, 10));

        var ranking = await RelatorioAsync();

        ranking.ShouldBe([new TopClienteLido(clienteId, "Parcial", 30m, 1)]);
    }

    [Fact]
    public async Task Returns_only_the_customers_with_orders_when_there_are_fewer_than_five()
    {
        var ana = await ClienteAsync("Ana");
        var bruno = await ClienteAsync("Bruno");
        await PedidoAsync(ana, 20m);
        await PedidoAsync(bruno, 30m);

        var ranking = await RelatorioAsync();

        ranking.ShouldBe(
            [
                new TopClienteLido(bruno, "Bruno", 30m, 1),
                new TopClienteLido(ana, "Ana", 20m, 1),
            ]);
    }

    [Fact]
    public async Task Returns_an_empty_list_when_no_order_is_in_the_period()
    {
        var clienteId = await ClienteAsync("Antigo");
        await PedidoAsync(clienteId, 10m, Utc(1, 10));

        var ranking = await RelatorioAsync();

        ranking.ShouldBeEmpty();
    }

    [Fact]
    public async Task Includes_orders_at_both_ends_of_the_period()
    {
        var clienteId = await ClienteAsync("Limites");
        await PedidoAsync(clienteId, 1000m, Utc(2, 28, 23, 59, 59, 999));
        await PedidoAsync(clienteId, 10m, Utc(3, 1, 0, 0, 0, 0));
        await PedidoAsync(clienteId, 20m, Utc(3, 31, 23, 59, 59, 999));
        await PedidoAsync(clienteId, 2000m, Utc(4, 1, 0, 0, 0, 0));

        var ranking = await RelatorioAsync();

        ranking.ShouldBe([new TopClienteLido(clienteId, "Limites", 30m, 2)]);
    }

    [Fact]
    public async Task Same_start_and_end_date_covers_the_whole_day()
    {
        var clienteId = await ClienteAsync("Um Dia");
        await PedidoAsync(clienteId, 1000m, Utc(3, 9, 23, 59, 59, 999));
        await PedidoAsync(clienteId, 1m, Utc(3, 10, 0, 0, 0, 0));
        await PedidoAsync(clienteId, 2m, Utc(3, 10, 12));
        await PedidoAsync(clienteId, 4m, Utc(3, 10, 23, 59, 59, 999));
        await PedidoAsync(clienteId, 2000m, Utc(3, 11, 0, 0, 0, 0));

        var ranking = await RelatorioAsync("dataInicio=2026-03-10&dataFim=2026-03-10");

        ranking.ShouldBe([new TopClienteLido(clienteId, "Um Dia", 7m, 3)]);
    }

    [Theory]
    [InlineData("dataFim=2026-03-31", "dataInicio", "periodo_obrigatorio")]
    [InlineData("dataInicio=2026-03-01", "dataFim", "periodo_obrigatorio")]
    [InlineData("dataInicio=2026-13-01&dataFim=2026-03-31", "dataInicio", "data_invalida")]
    [InlineData("dataInicio=2026-02-30&dataFim=2026-03-31", "dataInicio", "data_invalida")]
    [InlineData("dataInicio=01/03/2026&dataFim=2026-03-31", "dataInicio", "data_invalida")]
    [InlineData("dataInicio=abc&dataFim=2026-03-31", "dataInicio", "data_invalida")]
    [InlineData("dataInicio=2026-03-01&dataFim=2026-3-31", "dataFim", "data_invalida")]
    [InlineData("dataInicio=2026-03-01&dataFim=xyz", "dataFim", "data_invalida")]
    [InlineData("dataInicio=2026-04-01&dataFim=2026-03-31", "dataInicio", "periodo_invertido")]
    public async Task Rejects_invalid_period_with_the_field_code(string consulta, string campo, string code)
    {
        var resposta = await Api.TopClientesAsync(consulta);

        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.BadRequest, "validacao_falhou");
        var erros = problema.ErrosDeCampo();
        erros.Select(e => (e.Campo, e.Code)).ShouldBe([(campo, code)]);
        erros.ShouldAllBe(e => !string.IsNullOrWhiteSpace(e.Mensagem));
    }

    [Fact]
    public async Task Requires_both_dates_reporting_each_missing_one()
    {
        var resposta = await Api.GetAsync("/relatorios/top-clientes");

        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.BadRequest, "validacao_falhou");
        problema.ErrosDeCampo().Select(e => (e.Campo, e.Code)).ShouldBe(
            [("dataInicio", "periodo_obrigatorio"), ("dataFim", "periodo_obrigatorio")],
            ignoreOrder: true);
    }

    [Fact]
    public async Task Reports_an_invalid_date_and_a_missing_date_together()
    {
        var resposta = await Api.TopClientesAsync("dataInicio=abc");

        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.BadRequest, "validacao_falhou");
        problema.ErrosDeCampo().Select(e => (e.Campo, e.Code)).ShouldBe(
            [("dataInicio", "data_invalida"), ("dataFim", "periodo_obrigatorio")],
            ignoreOrder: true);
    }

    [Fact]
    public async Task Reports_both_invalid_dates_at_once()
    {
        var resposta = await Api.TopClientesAsync("dataInicio=2026-02-30&dataFim=abc");

        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.BadRequest, "validacao_falhou");
        problema.ErrosDeCampo().Select(e => (e.Campo, e.Code)).ShouldBe(
            [("dataInicio", "data_invalida"), ("dataFim", "data_invalida")],
            ignoreOrder: true);
    }
}
