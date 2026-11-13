using FluentValidation.Results;
using GestaoPedidos.Application.Relatorios.Queries.TopClientes;
using Shouldly;

namespace GestaoPedidos.UnitTests.Relatorios;

public sealed class TopClientesValidatorTests
{
    private static readonly DateOnly Inicio = new(2026, 3, 1);
    private static readonly DateOnly Fim = new(2026, 3, 31);

    private static ValidationResult Validar(DateOnly? dataInicio, DateOnly? dataFim)
    {
        return new TopClientesValidator().Validate(new TopClientesQuery(dataInicio, dataFim));
    }

    private static (string Campo, string Code)[] Erros(ValidationResult resultado)
    {
        resultado.Errors.ShouldAllBe(e => !string.IsNullOrWhiteSpace(e.ErrorMessage));
        return resultado.Errors.Select(e => (e.PropertyName, e.ErrorCode)).ToArray();
    }

    [Fact]
    public void Accepts_a_valid_period()
    {
        var resultado = Validar(Inicio, Fim);

        resultado.IsValid.ShouldBeTrue();
        resultado.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Accepts_the_same_start_and_end_date()
    {
        Validar(Inicio, Inicio).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Requires_the_start_date()
    {
        Erros(Validar(null, Fim)).ShouldBe([("dataInicio", "periodo_obrigatorio")]);
    }

    [Fact]
    public void Requires_the_end_date()
    {
        Erros(Validar(Inicio, null)).ShouldBe([("dataFim", "periodo_obrigatorio")]);
    }

    [Fact]
    public void Reports_both_missing_dates_at_once()
    {
        Erros(Validar(null, null)).ShouldBe(
            [("dataInicio", "periodo_obrigatorio"), ("dataFim", "periodo_obrigatorio")],
            ignoreOrder: true);
    }

    [Fact]
    public void Rejects_start_date_after_end_date()
    {
        Erros(Validar(Fim, Inicio)).ShouldBe([("dataInicio", "periodo_invertido")]);
    }
}
