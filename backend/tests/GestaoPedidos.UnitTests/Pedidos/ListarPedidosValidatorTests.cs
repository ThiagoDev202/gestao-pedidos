using FluentValidation.Results;
using GestaoPedidos.Application.Pedidos.Queries.ListarPedidos;
using Shouldly;

namespace GestaoPedidos.UnitTests.Pedidos;

public sealed class ListarPedidosValidatorTests
{
    private static readonly DateOnly Dia10 = new(2026, 3, 10);
    private static readonly DateOnly Dia11 = new(2026, 3, 11);

    private static ValidationResult Validar(
        int? clienteId = null,
        DateOnly? dataInicio = null,
        DateOnly? dataFim = null,
        int page = 1,
        int pageSize = 20)
    {
        return new ListarPedidosValidator().Validate(new ListarPedidosQuery(clienteId, dataInicio, dataFim, page, pageSize));
    }

    private static (string Campo, string Code)[] Erros(ValidationResult resultado)
    {
        resultado.Errors.ShouldAllBe(e => !string.IsNullOrWhiteSpace(e.ErrorMessage));
        return resultado.Errors.Select(e => (e.PropertyName, e.ErrorCode)).ToArray();
    }

    [Fact]
    public void Accepts_a_query_without_filters()
    {
        var resultado = Validar();

        resultado.IsValid.ShouldBeTrue();
        resultado.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Accepts_all_filters_together()
    {
        var resultado = Validar(clienteId: 1, dataInicio: Dia10, dataFim: Dia11, page: 3, pageSize: 50);

        resultado.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Accepts_the_same_start_and_end_date()
    {
        var resultado = Validar(dataInicio: Dia10, dataFim: Dia10);

        resultado.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Accepts_only_the_start_date()
    {
        Validar(dataInicio: Dia11).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Accepts_only_the_end_date()
    {
        Validar(dataFim: Dia10).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void Accepts_page_size_at_the_limits(int pageSize)
    {
        Validar(pageSize: pageSize).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Accepts_a_page_beyond_any_existing_data()
    {
        Validar(page: int.MaxValue).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Rejects_page_below_one(int page)
    {
        Erros(Validar(page: page)).ShouldBe([("page", "pagina_invalida")]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(101)]
    [InlineData(int.MaxValue)]
    public void Rejects_page_size_outside_one_to_one_hundred(int pageSize)
    {
        Erros(Validar(pageSize: pageSize)).ShouldBe([("pageSize", "tamanho_pagina_invalido")]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Rejects_customer_id_not_positive(int clienteId)
    {
        Erros(Validar(clienteId: clienteId)).ShouldBe([("clienteId", "cliente_id_invalido")]);
    }

    [Fact]
    public void Rejects_start_date_after_end_date()
    {
        Erros(Validar(dataInicio: Dia11, dataFim: Dia10)).ShouldBe([("dataInicio", "periodo_invertido")]);
    }

    [Fact]
    public void Reports_every_problem_at_once()
    {
        var resultado = Validar(clienteId: 0, dataInicio: Dia11, dataFim: Dia10, page: 0, pageSize: 101);

        Erros(resultado).ShouldBe(
            [
                ("page", "pagina_invalida"),
                ("pageSize", "tamanho_pagina_invalido"),
                ("clienteId", "cliente_id_invalido"),
                ("dataInicio", "periodo_invertido"),
            ],
            ignoreOrder: true);
    }
}
