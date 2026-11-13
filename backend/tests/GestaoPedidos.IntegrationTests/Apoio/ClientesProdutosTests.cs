using System.Net;
using System.Text.Json;
using GestaoPedidos.IntegrationTests.Support;
using Shouldly;

namespace GestaoPedidos.IntegrationTests.Apoio;

public sealed class ClientesProdutosTests(SqlServerFixture sql) : TesteDeIntegracao(sql)
{
    private sealed record ClienteLido(int Id, string Nome, string Email);

    private sealed record ProdutoLido(int Id, string Nome, string Sku, decimal Preco, int Estoque);

    private static async Task<IReadOnlyList<ClienteLido>> ClientesAsync(HttpClient api)
    {
        var corpo = await (await api.GetAsync("/clientes")).DeveSerOkAsync();
        corpo.ValueKind.ShouldBe(JsonValueKind.Array);
        return corpo.EnumerateArray()
            .Select(c => new ClienteLido(
                c.GetProperty("id").GetInt32(),
                c.GetProperty("nome").GetString()!,
                c.GetProperty("email").GetString()!))
            .ToList();
    }

    private static async Task<IReadOnlyList<ProdutoLido>> ProdutosAsync(HttpClient api)
    {
        var corpo = await (await api.GetAsync("/produtos")).DeveSerOkAsync();
        corpo.ValueKind.ShouldBe(JsonValueKind.Array);
        return corpo.EnumerateArray()
            .Select(p => new ProdutoLido(
                p.GetProperty("id").GetInt32(),
                p.GetProperty("nome").GetString()!,
                p.GetProperty("sku").GetString()!,
                p.GetProperty("preco").GetDecimal(),
                p.GetProperty("estoque").GetInt32()))
            .ToList();
    }

    [Fact]
    public async Task Lists_customers_ordered_by_name_then_id()
    {
        var bruno1 = await new ClienteBuilder().ComNome("Bruno").ComEmail("bruno1@teste.local").InserirAsync(Banco);
        var carla = await new ClienteBuilder().ComNome("Carla").ComEmail("carla@teste.local").InserirAsync(Banco);
        var ana = await new ClienteBuilder().ComNome("Ana").ComEmail("ana@teste.local").InserirAsync(Banco);
        var bruno2 = await new ClienteBuilder().ComNome("Bruno").ComEmail("bruno2@teste.local").InserirAsync(Banco);

        var clientes = await ClientesAsync(CriarCliente());

        clientes.ShouldBe(
            [
                new ClienteLido(ana, "Ana", "ana@teste.local"),
                new ClienteLido(bruno1, "Bruno", "bruno1@teste.local"),
                new ClienteLido(bruno2, "Bruno", "bruno2@teste.local"),
                new ClienteLido(carla, "Carla", "carla@teste.local"),
            ]);
    }

    [Fact]
    public async Task Returns_an_empty_customer_list_when_there_are_none()
    {
        var clientes = await ClientesAsync(CriarCliente());

        clientes.ShouldBeEmpty();
    }

    [Fact]
    public async Task Lists_products_with_sku_price_and_stock_ordered_by_name_then_id()
    {
        var teclado1 = await new ProdutoBuilder().ComNome("Teclado").ComSku("SKU-TEC-1").ComPreco(199.90m).ComEstoque(5).InserirAsync(Banco);
        var mouse = await new ProdutoBuilder().ComNome("Mouse").ComSku("SKU-MOU").ComPreco(49.50m).ComEstoque(0).InserirAsync(Banco);
        var teclado2 = await new ProdutoBuilder().ComNome("Teclado").ComSku("SKU-TEC-2").ComPreco(0m).ComEstoque(12).InserirAsync(Banco);
        var cabo = await new ProdutoBuilder().ComNome("Cabo USB").ComSku("SKU-CAB").ComPreco(15.05m).ComEstoque(300).InserirAsync(Banco);

        var produtos = await ProdutosAsync(CriarCliente());

        produtos.ShouldBe(
            [
                new ProdutoLido(cabo, "Cabo USB", "SKU-CAB", 15.05m, 300),
                new ProdutoLido(mouse, "Mouse", "SKU-MOU", 49.50m, 0),
                new ProdutoLido(teclado1, "Teclado", "SKU-TEC-1", 199.90m, 5),
                new ProdutoLido(teclado2, "Teclado", "SKU-TEC-2", 0m, 12),
            ]);
    }

    [Fact]
    public async Task Shows_the_current_stock_after_an_order_is_created()
    {
        var clienteId = await new ClienteBuilder().InserirAsync(Banco);
        var produtoId = await new ProdutoBuilder().ComNome("Monitor").ComSku("SKU-MON").ComPreco(900m).ComEstoque(8).InserirAsync(Banco);
        var api = CriarCliente();
        var criado = await api.CriarPedidoAsync(Respostas.Pedido(clienteId, (produtoId, 3)));
        criado.StatusCode.ShouldBe(HttpStatusCode.Created, await criado.Content.ReadAsStringAsync());

        var produtos = await ProdutosAsync(api);

        produtos.ShouldBe([new ProdutoLido(produtoId, "Monitor", "SKU-MON", 900m, 5)]);
    }

    [Fact]
    public async Task Returns_an_empty_product_list_when_there_are_none()
    {
        var produtos = await ProdutosAsync(CriarCliente());

        produtos.ShouldBeEmpty();
    }
}
