using FluentValidation.Results;
using GestaoPedidos.Application.Pedidos.Commands.CriarPedido;
using Shouldly;

namespace GestaoPedidos.UnitTests.Pedidos;

public sealed class CriarPedidoValidatorTests
{
    private static ValidationResult Validar(int clienteId, params (int ProdutoId, int Quantidade)[] itens)
    {
        var command = new CriarPedidoCommand(
            clienteId,
            itens.Select(i => new ItemDoPedidoCommand(i.ProdutoId, i.Quantidade)).ToList(),
            null);
        return new CriarPedidoValidator().Validate(command);
    }

    private static (string Campo, string Code)[] Erros(ValidationResult resultado)
    {
        resultado.Errors.ShouldAllBe(e => !string.IsNullOrWhiteSpace(e.ErrorMessage));
        return resultado.Errors.Select(e => (e.PropertyName, e.ErrorCode)).ToArray();
    }

    [Fact]
    public void Accepts_a_valid_order()
    {
        var resultado = Validar(1, (10, 2), (11, 1));

        resultado.IsValid.ShouldBeTrue();
        resultado.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Accepts_exactly_fifty_items()
    {
        var itens = Enumerable.Range(1, 50).Select(i => (i, 1)).ToArray();

        var resultado = Validar(1, itens);

        resultado.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Rejects_customer_id_not_positive(int clienteId)
    {
        var resultado = Validar(clienteId, (10, 1));

        Erros(resultado).ShouldBe([("clienteId", "cliente_id_invalido")]);
    }

    [Fact]
    public void Rejects_order_without_items()
    {
        var resultado = Validar(1);

        Erros(resultado).ShouldBe([("itens", "itens_vazio")]);
    }

    [Fact]
    public void Rejects_more_than_fifty_items()
    {
        var itens = Enumerable.Range(1, 51).Select(i => (i, 1)).ToArray();

        var resultado = Validar(1, itens);

        Erros(resultado).ShouldBe([("itens", "itens_excedem_limite")]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Rejects_product_id_not_positive_pointing_to_the_item(int produtoId)
    {
        var resultado = Validar(1, (10, 1), (produtoId, 1));

        Erros(resultado).ShouldBe([("itens[1].produtoId", "produto_id_invalido")]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void Rejects_quantity_not_positive_pointing_to_the_item(int quantidade)
    {
        var resultado = Validar(1, (10, quantidade), (11, 1));

        Erros(resultado).ShouldBe([("itens[0].quantidade", "quantidade_invalida")]);
    }

    [Fact]
    public void Rejects_repeated_product_pointing_to_its_second_occurrence()
    {
        var resultado = Validar(1, (5, 1), (7, 1), (5, 2));

        Erros(resultado).ShouldBe([("itens[2].produtoId", "produto_repetido")]);
    }

    [Fact]
    public void Reports_every_problem_at_once()
    {
        var resultado = Validar(0, (0, 1), (3, 0), (3, 2));

        Erros(resultado).ShouldBe(
            [
                ("clienteId", "cliente_id_invalido"),
                ("itens[0].produtoId", "produto_id_invalido"),
                ("itens[1].quantidade", "quantidade_invalida"),
                ("itens[2].produtoId", "produto_repetido"),
            ],
            ignoreOrder: true);
    }

    [Fact]
    public void Reports_missing_customer_and_empty_items_together()
    {
        var resultado = Validar(0);

        Erros(resultado).ShouldBe([("clienteId", "cliente_id_invalido"), ("itens", "itens_vazio")], ignoreOrder: true);
    }
}
