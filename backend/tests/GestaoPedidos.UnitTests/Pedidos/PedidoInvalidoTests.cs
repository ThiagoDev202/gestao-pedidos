using GestaoPedidos.Domain.Excecoes;
using GestaoPedidos.Domain.Pedidos;
using Shouldly;

namespace GestaoPedidos.UnitTests.Pedidos;

public sealed class PedidoInvalidoTests
{
    private static readonly DateTime Agora = new(2026, 5, 20, 18, 30, 15, DateTimeKind.Utc);

    [Fact]
    public void Order_without_items_reports_empty_items_code_on_items_field()
    {
        var excecao = Should.Throw<PedidoInvalidoException>(() => Pedido.Criar(7, [], Agora));

        excecao.Codigo.ShouldBe("itens_vazio");
        excecao.Campo.ShouldBe("itens");
        excecao.Message.ShouldNotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Item_with_quantity_not_positive_reports_invalid_quantity_code_on_items_field(int quantidade)
    {
        var excecao = Should.Throw<PedidoInvalidoException>(() =>
            Pedido.Criar(7, [new ItemParaPedido(1, 1, 5m), new ItemParaPedido(2, quantidade, 5m)], Agora));

        excecao.Codigo.ShouldBe("quantidade_invalida");
        excecao.Campo.ShouldBe("itens");
        excecao.Message.ShouldNotBeNullOrWhiteSpace();
    }
}
