using System.Net;
using GestaoPedidos.IntegrationTests.Support;
using Shouldly;

namespace GestaoPedidos.IntegrationTests.Pedidos;

public sealed class ObterPedidoTests(SqlServerFixture sql) : TesteDeIntegracao(sql)
{
    private sealed record ItemLido(int ProdutoId, string ProdutoNome, string Sku, int Quantidade, decimal PrecoUnitario, decimal Subtotal);

    [Fact]
    public async Task Returns_the_order_detail_with_items_ordered_by_product_and_their_subtotals()
    {
        var clienteId = await new ClienteBuilder().ComNome("Helena Detalhe").InserirAsync(Banco);
        var cabo = await new ProdutoBuilder().ComNome("Cabo HDMI").ComSku("SKU-HDMI").ComPreco(30m).InserirAsync(Banco);
        var hub = await new ProdutoBuilder().ComNome("Hub USB").ComSku("SKU-HUB").ComPreco(80m).InserirAsync(Banco);
        var data = new DateTime(2026, 3, 10, 23, 59, 59, 999, DateTimeKind.Utc);
        var pedidoId = await new PedidoBuilder(clienteId)
            .ComItem(hub, 2, 79.90m)
            .ComItem(cabo, 3, 25.15m)
            .InserirEmAsync(Banco, data);

        var corpo = await (await CriarCliente().GetAsync($"/pedidos/{pedidoId}")).DeveSerOkAsync();

        corpo.GetProperty("id").GetInt32().ShouldBe(pedidoId);
        corpo.GetProperty("clienteId").GetInt32().ShouldBe(clienteId);
        corpo.GetProperty("clienteNome").GetString().ShouldBe("Helena Detalhe");
        corpo.DataUtc("dataPedido").ShouldBe(data);
        corpo.GetProperty("valorTotal").GetDecimal().ShouldBe(235.25m);
        var itens = corpo.GetProperty("itens").EnumerateArray()
            .Select(i => new ItemLido(
                i.GetProperty("produtoId").GetInt32(),
                i.GetProperty("produtoNome").GetString()!,
                i.GetProperty("sku").GetString()!,
                i.GetProperty("quantidade").GetInt32(),
                i.GetProperty("precoUnitario").GetDecimal(),
                i.GetProperty("subtotal").GetDecimal()))
            .ToList();
        itens.ShouldBe(
            [
                new ItemLido(cabo, "Cabo HDMI", "SKU-HDMI", 3, 25.15m, 75.45m),
                new ItemLido(hub, "Hub USB", "SKU-HUB", 2, 79.90m, 159.80m),
            ]);
    }

    [Fact]
    public async Task Returns_the_order_created_through_the_api()
    {
        var clienteId = await new ClienteBuilder().ComNome("Igor Criado").InserirAsync(Banco);
        var produtoId = await new ProdutoBuilder().ComNome("Webcam").ComSku("SKU-CAM").ComPreco(120.40m).ComEstoque(4).InserirAsync(Banco);
        var api = CriarApi();
        api.Relogio.UtcNow = new DateTime(2026, 5, 2, 8, 30, 0, 450, DateTimeKind.Utc);
        var cliente = api.CreateClient();
        var criado = await cliente.CriarPedidoAsync(Respostas.Pedido(clienteId, (produtoId, 2)));
        criado.StatusCode.ShouldBe(HttpStatusCode.Created, await criado.Content.ReadAsStringAsync());
        var localizacao = criado.Headers.Location!.OriginalString;

        var corpo = await (await cliente.GetAsync(localizacao)).DeveSerOkAsync();

        corpo.GetProperty("clienteNome").GetString().ShouldBe("Igor Criado");
        corpo.DataUtc("dataPedido").ShouldBe(api.Relogio.UtcNow);
        corpo.GetProperty("valorTotal").GetDecimal().ShouldBe(240.80m);
        var item = corpo.GetProperty("itens").EnumerateArray().Single();
        item.GetProperty("produtoId").GetInt32().ShouldBe(produtoId);
        item.GetProperty("quantidade").GetInt32().ShouldBe(2);
        item.GetProperty("precoUnitario").GetDecimal().ShouldBe(120.40m);
        item.GetProperty("subtotal").GetDecimal().ShouldBe(240.80m);
    }

    [Fact]
    public async Task Returns_404_with_the_order_id_when_the_order_does_not_exist()
    {
        var clienteId = await new ClienteBuilder().InserirAsync(Banco);
        var produtoId = await new ProdutoBuilder().InserirAsync(Banco);
        var existente = await new PedidoBuilder(clienteId).ComItem(produtoId, 1, 10m).InserirAsync(Banco);
        var inexistente = existente + 1000;

        var resposta = await CriarCliente().GetAsync($"/pedidos/{inexistente}");

        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.NotFound, "pedido_nao_encontrado");
        problema.GetProperty("meta").GetProperty("pedidoId").GetInt32().ShouldBe(inexistente);
    }

    [Fact]
    public async Task Returns_404_resource_not_found_for_a_non_numeric_id()
    {
        var resposta = await CriarCliente().GetAsync("/pedidos/abc");

        await resposta.DeveSerProblemaAsync(HttpStatusCode.NotFound, "recurso_nao_encontrado");
    }
}
