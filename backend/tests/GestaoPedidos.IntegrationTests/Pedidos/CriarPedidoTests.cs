using System.Globalization;
using System.Net;
using System.Text.Json;
using GestaoPedidos.IntegrationTests.Support;
using Shouldly;

namespace GestaoPedidos.IntegrationTests.Pedidos;

public sealed class CriarPedidoTests(SqlServerFixture sql) : TesteDeIntegracao(sql)
{
    private async Task<(int ClienteId, int ProdutoA, int ProdutoB)> CenarioAsync(int estoqueA = 10, int estoqueB = 5)
    {
        var clienteId = await new ClienteBuilder().ComNome("Maria Teste").InserirAsync(Banco);
        var produtoA = await new ProdutoBuilder().ComNome("Teclado Mecânico").ComSku("SKU-TEC").ComPreco(10.50m).ComEstoque(estoqueA).InserirAsync(Banco);
        var produtoB = await new ProdutoBuilder().ComNome("Mouse Óptico").ComSku("SKU-MOU").ComPreco(3.25m).ComEstoque(estoqueB).InserirAsync(Banco);
        return (clienteId, produtoA, produtoB);
    }

    private async Task NadaFoiGravadoAsync()
    {
        (await Banco.ContarPedidosAsync()).ShouldBe(0);
        (await Banco.ContarItensAsync()).ShouldBe(0);
        (await Banco.ContarRegistrosDeIdempotenciaAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task Creates_order_and_returns_201_with_location_and_details()
    {
        var (clienteId, produtoA, produtoB) = await CenarioAsync();
        var api = CriarApi();

        var resposta = await api.CreateClient().CriarPedidoAsync(Respostas.Pedido(clienteId, (produtoB, 2), (produtoA, 3)));

        resposta.StatusCode.ShouldBe(HttpStatusCode.Created, await resposta.Content.ReadAsStringAsync());
        (resposta.Content.Headers.ContentType?.MediaType).ShouldBe("application/json");
        var corpo = await resposta.JsonAsync();
        var id = corpo.GetProperty("id").GetInt32();
        id.ShouldBeGreaterThan(0);
        resposta.Headers.Location.ShouldNotBeNull();
        resposta.Headers.Location.OriginalString.ShouldBe($"/pedidos/{id}");
        corpo.GetProperty("clienteId").GetInt32().ShouldBe(clienteId);
        corpo.GetProperty("clienteNome").GetString().ShouldBe("Maria Teste");
        corpo.GetProperty("valorTotal").GetDecimal().ShouldBe(38.00m);

        var itens = corpo.GetProperty("itens").EnumerateArray().ToList();
        itens.Count.ShouldBe(2);
        var itemA = itens.Single(i => i.GetProperty("produtoId").GetInt32() == produtoA);
        itemA.GetProperty("produtoNome").GetString().ShouldBe("Teclado Mecânico");
        itemA.GetProperty("sku").GetString().ShouldBe("SKU-TEC");
        itemA.GetProperty("quantidade").GetInt32().ShouldBe(3);
        itemA.GetProperty("precoUnitario").GetDecimal().ShouldBe(10.50m);
        itemA.GetProperty("subtotal").GetDecimal().ShouldBe(31.50m);
        var itemB = itens.Single(i => i.GetProperty("produtoId").GetInt32() == produtoB);
        itemB.GetProperty("produtoNome").GetString().ShouldBe("Mouse Óptico");
        itemB.GetProperty("sku").GetString().ShouldBe("SKU-MOU");
        itemB.GetProperty("quantidade").GetInt32().ShouldBe(2);
        itemB.GetProperty("precoUnitario").GetDecimal().ShouldBe(3.25m);
        itemB.GetProperty("subtotal").GetDecimal().ShouldBe(6.50m);
    }

    [Fact]
    public async Task Returns_order_date_in_utc_set_by_the_server_clock()
    {
        var (clienteId, produtoA, _) = await CenarioAsync();
        var api = CriarApi();
        api.Relogio.UtcNow = new DateTime(2026, 7, 1, 23, 59, 58, 345, DateTimeKind.Utc);

        var resposta = await api.CreateClient().CriarPedidoAsync(Respostas.Pedido(clienteId, (produtoA, 1)));

        resposta.StatusCode.ShouldBe(HttpStatusCode.Created, await resposta.Content.ReadAsStringAsync());
        var texto = (await resposta.JsonAsync()).GetProperty("dataPedido").GetString()!;
        (texto.EndsWith('Z') || texto.EndsWith("+00:00", StringComparison.Ordinal)).ShouldBeTrue($"dataPedido sem fuso UTC explícito: {texto}");
        var data = DateTimeOffset.Parse(texto, CultureInfo.InvariantCulture, DateTimeStyles.None);
        data.Offset.ShouldBe(TimeSpan.Zero);
        data.UtcDateTime.ShouldBe(api.Relogio.UtcNow);
    }

    [Fact]
    public async Task Persists_order_items_frozen_prices_total_and_decrements_stock()
    {
        var (clienteId, produtoA, produtoB) = await CenarioAsync();
        var api = CriarApi();

        var resposta = await api.CreateClient().CriarPedidoAsync(Respostas.Pedido(clienteId, (produtoB, 2), (produtoA, 3)));

        resposta.StatusCode.ShouldBe(HttpStatusCode.Created, await resposta.Content.ReadAsStringAsync());
        var id = (await resposta.JsonAsync()).GetProperty("id").GetInt32();
        var gravado = await Banco.PedidoAsync(id);
        gravado.ClienteId.ShouldBe(clienteId);
        gravado.DataPedido.ShouldBe(FakeClock.Padrao);
        gravado.ValorTotal.ShouldBe(38.00m);
        gravado.Itens.ShouldBe(
            [new ItemGravado(produtoA, 3, 10.50m), new ItemGravado(produtoB, 2, 3.25m)],
            ignoreOrder: true);
        (await Banco.EstoqueAsync(produtoA)).ShouldBe(7);
        (await Banco.EstoqueAsync(produtoB)).ShouldBe(3);
    }

    [Fact]
    public async Task Accepts_order_that_takes_the_whole_stock()
    {
        var (clienteId, produtoA, _) = await CenarioAsync(estoqueA: 4);

        var resposta = await CriarCliente().CriarPedidoAsync(Respostas.Pedido(clienteId, (produtoA, 4)));

        resposta.StatusCode.ShouldBe(HttpStatusCode.Created, await resposta.Content.ReadAsStringAsync());
        (await Banco.EstoqueAsync(produtoA)).ShouldBe(0);
    }

    [Fact]
    public async Task Keeps_the_original_price_after_the_product_price_changes()
    {
        var (clienteId, produtoA, _) = await CenarioAsync();
        var cliente = CriarCliente();
        var criado = await cliente.CriarPedidoAsync(Respostas.Pedido(clienteId, (produtoA, 2)));
        criado.StatusCode.ShouldBe(HttpStatusCode.Created, await criado.Content.ReadAsStringAsync());
        var id = (await criado.JsonAsync()).GetProperty("id").GetInt32();

        await Banco.ExecutarAsync("UPDATE dbo.Produtos SET Preco = 99.90 WHERE Id = @id", ("@id", produtoA));
        var consulta = await cliente.GetAsync($"/pedidos/{id}");

        consulta.StatusCode.ShouldBe(HttpStatusCode.OK, await consulta.Content.ReadAsStringAsync());
        var corpo = await consulta.JsonAsync();
        corpo.GetProperty("valorTotal").GetDecimal().ShouldBe(21.00m);
        var item = corpo.GetProperty("itens").EnumerateArray().Single();
        item.GetProperty("precoUnitario").GetDecimal().ShouldBe(10.50m);
        item.GetProperty("subtotal").GetDecimal().ShouldBe(21.00m);
        (await Banco.PedidoAsync(id)).Itens.Single().PrecoUnitario.ShouldBe(10.50m);
    }

    [Fact]
    public async Task Rejects_a_price_sent_by_the_client_as_an_unknown_field()
    {
        var (clienteId, produtoA, _) = await CenarioAsync();

        var resposta = await CriarCliente().CriarPedidoAsync(new
        {
            clienteId,
            itens = new[] { new { produtoId = produtoA, quantidade = 1, precoUnitario = 0.01m } },
        });

        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.BadRequest, "validacao_falhou");
        problema.ErrosDeCampo().Select(e => (e.Campo, e.Code)).ShouldBe([("$", "corpo_invalido")]);
        await NadaFoiGravadoAsync();
        (await Banco.EstoqueAsync(produtoA)).ShouldBe(10);
    }

    [Fact]
    public async Task Rejects_missing_customer_with_422_and_customer_id()
    {
        var (_, produtoA, _) = await CenarioAsync();

        var resposta = await CriarCliente().CriarPedidoAsync(Respostas.Pedido(999_999, (produtoA, 1)));

        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.UnprocessableEntity, "cliente_inexistente");
        problema.GetProperty("meta").GetProperty("clienteId").GetInt32().ShouldBe(999_999);
        await NadaFoiGravadoAsync();
        (await Banco.EstoqueAsync(produtoA)).ShouldBe(10);
    }

    [Fact]
    public async Task Rejects_missing_product_with_422_and_product_id()
    {
        var (clienteId, produtoA, _) = await CenarioAsync();

        var resposta = await CriarCliente().CriarPedidoAsync(Respostas.Pedido(clienteId, (produtoA, 1), (999_999, 1)));

        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.UnprocessableEntity, "produto_inexistente");
        problema.GetProperty("meta").GetProperty("produtoId").GetInt32().ShouldBe(999_999);
        await NadaFoiGravadoAsync();
        (await Banco.EstoqueAsync(produtoA)).ShouldBe(10);
    }

    [Fact]
    public async Task Rejects_insufficient_stock_with_422_and_leaves_every_stock_untouched()
    {
        var (clienteId, produtoA, produtoB) = await CenarioAsync(estoqueA: 10, estoqueB: 1);

        var resposta = await CriarCliente().CriarPedidoAsync(Respostas.Pedido(clienteId, (produtoA, 2), (produtoB, 5)));

        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.UnprocessableEntity, "estoque_insuficiente");
        var meta = problema.GetProperty("meta");
        meta.GetProperty("produtoId").GetInt32().ShouldBe(produtoB);
        meta.GetProperty("produtoNome").GetString().ShouldBe("Mouse Óptico");
        meta.GetProperty("solicitado").GetInt32().ShouldBe(5);
        meta.GetProperty("disponivel").GetInt32().ShouldBe(1);
        problema.GetProperty("detail").GetString()!.ShouldContain("Mouse Óptico");
        await NadaFoiGravadoAsync();
        (await Banco.EstoqueAsync(produtoA)).ShouldBe(10);
        (await Banco.EstoqueAsync(produtoB)).ShouldBe(1);
    }

    [Fact]
    public async Task Reports_the_first_product_without_stock_in_product_id_order()
    {
        var (clienteId, produtoA, produtoB) = await CenarioAsync(estoqueA: 1, estoqueB: 1);

        var resposta = await CriarCliente().CriarPedidoAsync(Respostas.Pedido(clienteId, (produtoB, 3), (produtoA, 2)));

        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.UnprocessableEntity, "estoque_insuficiente");
        problema.GetProperty("meta").GetProperty("produtoId").GetInt32().ShouldBe(Math.Min(produtoA, produtoB));
        await NadaFoiGravadoAsync();
    }

    [Fact]
    public async Task Rejects_reused_idempotency_key_with_a_different_body()
    {
        var (clienteId, produtoA, produtoB) = await CenarioAsync();
        var cliente = CriarCliente();
        var chave = Guid.NewGuid().ToString();
        var primeira = await cliente.CriarPedidoAsync(Respostas.Pedido(clienteId, (produtoA, 1)), chave);
        primeira.StatusCode.ShouldBe(HttpStatusCode.Created, await primeira.Content.ReadAsStringAsync());

        var resposta = await cliente.CriarPedidoAsync(Respostas.Pedido(clienteId, (produtoB, 1)), chave);

        await resposta.DeveSerProblemaAsync(HttpStatusCode.UnprocessableEntity, "chave_reutilizada");
        (await Banco.ContarPedidosAsync()).ShouldBe(1);
        (await Banco.EstoqueAsync(produtoA)).ShouldBe(9);
        (await Banco.EstoqueAsync(produtoB)).ShouldBe(5);
    }

    public static TheoryData<string, string, string> ErrosDeCampoIsolados => new()
    {
        { """{"clienteId":0,"itens":[{"produtoId":1,"quantidade":1}]}""", "clienteId", "cliente_id_invalido" },
        { """{"clienteId":-1,"itens":[{"produtoId":1,"quantidade":1}]}""", "clienteId", "cliente_id_invalido" },
        { """{"clienteId":1,"itens":[]}""", "itens", "itens_vazio" },
        { """{"clienteId":1,"itens":[{"produtoId":1,"quantidade":1},{"produtoId":0,"quantidade":1}]}""", "itens[1].produtoId", "produto_id_invalido" },
        { """{"clienteId":1,"itens":[{"produtoId":1,"quantidade":0}]}""", "itens[0].quantidade", "quantidade_invalida" },
        { """{"clienteId":1,"itens":[{"produtoId":1,"quantidade":-4}]}""", "itens[0].quantidade", "quantidade_invalida" },
        { """{"clienteId":1,"itens":[{"produtoId":1,"quantidade":1},{"produtoId":2,"quantidade":1},{"produtoId":1,"quantidade":2}]}""", "itens[2].produtoId", "produto_repetido" },
    };

    [Theory]
    [MemberData(nameof(ErrosDeCampoIsolados))]
    public async Task Rejects_invalid_field_with_400_and_the_field_code(string json, string campo, string code)
    {
        var resposta = await CriarCliente().PostAsync("/pedidos", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.BadRequest, "validacao_falhou");
        var erros = problema.ErrosDeCampo();
        erros.Select(e => (e.Campo, e.Code)).ShouldBe([(campo, code)]);
        erros.ShouldAllBe(e => !string.IsNullOrWhiteSpace(e.Mensagem));
        await NadaFoiGravadoAsync();
    }

    [Fact]
    public async Task Rejects_more_than_fifty_items_with_400()
    {
        var itens = Enumerable.Range(1, 51).Select(i => (i, 1)).ToArray();

        var resposta = await CriarCliente().CriarPedidoAsync(Respostas.Pedido(1, itens));

        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.BadRequest, "validacao_falhou");
        problema.ErrosDeCampo().Select(e => (e.Campo, e.Code)).ShouldBe([("itens", "itens_excedem_limite")]);
    }

    [Fact]
    public async Task Rejects_idempotency_key_that_is_not_a_uuid_with_400()
    {
        var (clienteId, produtoA, _) = await CenarioAsync();

        var resposta = await CriarCliente().CriarPedidoAsync(Respostas.Pedido(clienteId, (produtoA, 1)), "nao-e-uuid");

        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.BadRequest, "validacao_falhou");
        problema.ErrosDeCampo().Select(e => (e.Campo, e.Code)).ShouldBe([("Idempotency-Key", "chave_idempotencia_invalida")]);
        await NadaFoiGravadoAsync();
        (await Banco.EstoqueAsync(produtoA)).ShouldBe(10);
    }

    [Fact]
    public async Task Reports_every_validation_problem_at_once()
    {
        var corpo = new
        {
            clienteId = 0,
            itens = new[]
            {
                new { produtoId = 0, quantidade = 1 },
                new { produtoId = 3, quantidade = 0 },
                new { produtoId = 3, quantidade = 2 },
            },
        };

        var resposta = await CriarCliente().CriarPedidoAsync(corpo, "123");

        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.BadRequest, "validacao_falhou");
        problema.ErrosDeCampo().Select(e => (e.Campo, e.Code)).ShouldBe(
            [
                ("clienteId", "cliente_id_invalido"),
                ("itens[0].produtoId", "produto_id_invalido"),
                ("itens[1].quantidade", "quantidade_invalida"),
                ("itens[2].produtoId", "produto_repetido"),
                ("Idempotency-Key", "chave_idempotencia_invalida"),
            ],
            ignoreOrder: true);
    }

    [Fact]
    public async Task Validates_before_checking_business_rules()
    {
        var resposta = await CriarCliente().CriarPedidoAsync(Respostas.Pedido(999_999, (999_999, 0)));

        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.BadRequest, "validacao_falhou");
        problema.ErrosDeCampo().Select(e => e.Code).ShouldBe(["quantidade_invalida"]);
    }

    [Fact]
    public async Task Uses_camel_case_json_for_the_error_body()
    {
        var resposta = await CriarCliente().CriarPedidoAsync(Respostas.Pedido(0, (1, 1)));

        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.BadRequest, "validacao_falhou");
        problema.EnumerateObject().Select(p => p.Name).ShouldAllBe(nome => char.IsLower(nome[0]));
        problema.GetProperty("errors").ValueKind.ShouldBe(JsonValueKind.Array);
    }
}
