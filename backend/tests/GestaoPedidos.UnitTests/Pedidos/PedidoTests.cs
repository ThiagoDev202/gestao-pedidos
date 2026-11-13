using GestaoPedidos.Domain.Excecoes;
using GestaoPedidos.Domain.Pedidos;
using Shouldly;

namespace GestaoPedidos.UnitTests.Pedidos;

public sealed class PedidoTests
{
    private static readonly DateTime Agora = new(2026, 5, 20, 18, 30, 15, 250, DateTimeKind.Utc);

    [Fact]
    public void Creates_order_for_the_customer_with_the_given_items()
    {
        var pedido = Pedido.Criar(7, [new ItemParaPedido(1, 2, 10.50m), new ItemParaPedido(2, 3, 4.25m)], Agora);

        pedido.ClienteId.ShouldBe(7);
        pedido.Itens.Count.ShouldBe(2);
        pedido.Itens.Select(i => (i.ProdutoId, i.Quantidade)).ShouldBe([(1, 2), (2, 3)], ignoreOrder: true);
    }

    [Fact]
    public void Calculates_total_as_sum_of_quantity_times_unit_price()
    {
        var pedido = Pedido.Criar(7, [new ItemParaPedido(1, 2, 10.50m), new ItemParaPedido(2, 3, 4.25m), new ItemParaPedido(3, 1, 0.99m)], Agora);

        pedido.ValorTotal.ShouldBe(34.74m);
    }

    [Fact]
    public void Calculates_item_subtotal()
    {
        var pedido = Pedido.Criar(7, [new ItemParaPedido(1, 4, 2.35m)], Agora);

        pedido.Itens.Single().Subtotal.ShouldBe(9.40m);
    }

    [Fact]
    public void Keeps_the_unit_price_given_at_creation()
    {
        var pedido = Pedido.Criar(7, [new ItemParaPedido(1, 1, 99.99m), new ItemParaPedido(2, 5, 0.01m)], Agora);

        pedido.Itens.Single(i => i.ProdutoId == 1).PrecoUnitario.ShouldBe(99.99m);
        pedido.Itens.Single(i => i.ProdutoId == 2).PrecoUnitario.ShouldBe(0.01m);
    }

    [Fact]
    public void Sets_order_date_to_the_given_utc_instant()
    {
        var pedido = Pedido.Criar(7, [new ItemParaPedido(1, 1, 1m)], Agora);

        pedido.DataPedido.ShouldBe(Agora);
        pedido.DataPedido.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public void Rejects_order_without_items()
    {
        Should.Throw<PedidoInvalidoException>(() => Pedido.Criar(7, [], Agora));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Rejects_item_with_quantity_not_positive(int quantidade)
    {
        Should.Throw<PedidoInvalidoException>(() =>
            Pedido.Criar(7, [new ItemParaPedido(1, 1, 5m), new ItemParaPedido(2, quantidade, 5m)], Agora));
    }
}
