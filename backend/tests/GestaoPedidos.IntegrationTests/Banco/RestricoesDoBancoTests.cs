using GestaoPedidos.IntegrationTests.Support;
using Microsoft.Data.SqlClient;
using Shouldly;

namespace GestaoPedidos.IntegrationTests.Banco;

public sealed class RestricoesDoBancoTests(SqlServerFixture sql) : TesteDeIntegracao(sql)
{
    private const int ViolacaoDeCheckOuFk = 547;
    private static readonly int[] ViolacaoDeUnicidade = [2627, 2601];

    [Fact]
    public async Task Rejects_negative_stock()
    {
        var erro = await Should.ThrowAsync<SqlException>(() => new ProdutoBuilder().ComEstoque(-1).InserirAsync(Banco));

        erro.Number.ShouldBe(ViolacaoDeCheckOuFk);
    }

    [Fact]
    public async Task Rejects_stock_update_that_would_go_below_zero()
    {
        var produtoId = await new ProdutoBuilder().ComEstoque(1).InserirAsync(Banco);

        var erro = await Should.ThrowAsync<SqlException>(() =>
            Banco.ExecutarAsync("UPDATE dbo.Produtos SET Estoque = Estoque - 2 WHERE Id = @id", ("@id", produtoId)));

        erro.Number.ShouldBe(ViolacaoDeCheckOuFk);
        (await Banco.EstoqueAsync(produtoId)).ShouldBe(1);
    }

    [Fact]
    public async Task Accepts_zero_stock_and_zero_price()
    {
        var produtoId = await new ProdutoBuilder().ComEstoque(0).ComPreco(0m).InserirAsync(Banco);

        (await Banco.EstoqueAsync(produtoId)).ShouldBe(0);
        (await Banco.PrecoAsync(produtoId)).ShouldBe(0m);
    }

    [Fact]
    public async Task Rejects_negative_price()
    {
        var erro = await Should.ThrowAsync<SqlException>(() => new ProdutoBuilder().ComPreco(-0.01m).InserirAsync(Banco));

        erro.Number.ShouldBe(ViolacaoDeCheckOuFk);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Rejects_item_quantity_that_is_not_positive(int quantidade)
    {
        var clienteId = await new ClienteBuilder().InserirAsync(Banco);
        var produtoId = await new ProdutoBuilder().InserirAsync(Banco);

        var erro = await Should.ThrowAsync<SqlException>(() =>
            new PedidoBuilder(clienteId).ComItem(produtoId, quantidade, 10m).InserirAsync(Banco));

        erro.Number.ShouldBe(ViolacaoDeCheckOuFk);
    }

    [Fact]
    public async Task Rejects_negative_unit_price_on_item()
    {
        var clienteId = await new ClienteBuilder().InserirAsync(Banco);
        var produtoId = await new ProdutoBuilder().InserirAsync(Banco);
        var pedidoId = await new PedidoBuilder(clienteId).InserirAsync(Banco);

        var erro = await Should.ThrowAsync<SqlException>(() => Banco.ExecutarAsync(
            "INSERT INTO dbo.ItensPedido (PedidoId, ProdutoId, Quantidade, PrecoUnitario) VALUES (@pedido, @produto, 1, -1)",
            ("@pedido", pedidoId),
            ("@produto", produtoId)));

        erro.Number.ShouldBe(ViolacaoDeCheckOuFk);
    }

    [Fact]
    public async Task Rejects_negative_order_total()
    {
        var clienteId = await new ClienteBuilder().InserirAsync(Banco);

        var erro = await Should.ThrowAsync<SqlException>(() => Banco.ExecutarAsync(
            "INSERT INTO dbo.Pedidos (ClienteId, DataPedido, ValorTotal) VALUES (@cliente, SYSUTCDATETIME(), -1)",
            ("@cliente", clienteId)));

        erro.Number.ShouldBe(ViolacaoDeCheckOuFk);
    }

    [Fact]
    public async Task Rejects_duplicated_customer_email()
    {
        await new ClienteBuilder().ComEmail("repetido@teste.local").InserirAsync(Banco);

        var erro = await Should.ThrowAsync<SqlException>(() =>
            new ClienteBuilder().ComEmail("repetido@teste.local").InserirAsync(Banco));

        ViolacaoDeUnicidade.ShouldContain(erro.Number);
    }

    [Fact]
    public async Task Accepts_customers_with_the_same_name()
    {
        await new ClienteBuilder().ComNome("Ana Silva").InserirAsync(Banco);
        await new ClienteBuilder().ComNome("Ana Silva").InserirAsync(Banco);

        (await Banco.EscalarAsync<int>("SELECT COUNT(*) FROM dbo.Clientes WHERE Nome = N'Ana Silva'")).ShouldBe(2);
    }

    [Fact]
    public async Task Rejects_duplicated_product_sku()
    {
        await new ProdutoBuilder().ComSku("SKU-DUP").InserirAsync(Banco);

        var erro = await Should.ThrowAsync<SqlException>(() => new ProdutoBuilder().ComSku("SKU-DUP").InserirAsync(Banco));

        ViolacaoDeUnicidade.ShouldContain(erro.Number);
    }

    [Fact]
    public async Task Rejects_the_same_product_twice_in_one_order()
    {
        var clienteId = await new ClienteBuilder().InserirAsync(Banco);
        var produtoId = await new ProdutoBuilder().InserirAsync(Banco);

        var erro = await Should.ThrowAsync<SqlException>(() =>
            new PedidoBuilder(clienteId).ComItem(produtoId, 1, 10m).ComItem(produtoId, 2, 10m).InserirAsync(Banco));

        ViolacaoDeUnicidade.ShouldContain(erro.Number);
    }

    [Fact]
    public async Task Rejects_order_for_missing_customer()
    {
        var erro = await Should.ThrowAsync<SqlException>(() => new PedidoBuilder(999_999).InserirAsync(Banco));

        erro.Number.ShouldBe(ViolacaoDeCheckOuFk);
    }

    [Fact]
    public async Task Rejects_item_for_missing_product()
    {
        var clienteId = await new ClienteBuilder().InserirAsync(Banco);

        var erro = await Should.ThrowAsync<SqlException>(() =>
            new PedidoBuilder(clienteId).ComItem(999_999, 1, 10m).InserirAsync(Banco));

        erro.Number.ShouldBe(ViolacaoDeCheckOuFk);
    }

    [Fact]
    public async Task Rejects_idempotency_record_for_missing_order()
    {
        var erro = await Should.ThrowAsync<SqlException>(() => Banco.ExecutarAsync(
            "INSERT INTO dbo.IdempotenciaPedidos (Chave, HashCorpo, PedidoId) VALUES (NEWID(), REPLICATE('a', 64), 999999)"));

        erro.Number.ShouldBe(ViolacaoDeCheckOuFk);
    }

    [Fact]
    public async Task Rejects_reused_idempotency_key()
    {
        var clienteId = await new ClienteBuilder().InserirAsync(Banco);
        var pedidoId = await new PedidoBuilder(clienteId).InserirAsync(Banco);
        var chave = Guid.NewGuid();
        const string Sql = "INSERT INTO dbo.IdempotenciaPedidos (Chave, HashCorpo, PedidoId) VALUES (@chave, REPLICATE('a', 64), @pedido)";
        await Banco.ExecutarAsync(Sql, ("@chave", chave), ("@pedido", pedidoId));

        var erro = await Should.ThrowAsync<SqlException>(() => Banco.ExecutarAsync(Sql, ("@chave", chave), ("@pedido", pedidoId)));

        ViolacaoDeUnicidade.ShouldContain(erro.Number);
    }
}
