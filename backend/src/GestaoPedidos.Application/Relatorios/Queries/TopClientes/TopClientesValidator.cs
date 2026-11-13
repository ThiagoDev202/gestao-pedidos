using FluentValidation;

namespace GestaoPedidos.Application.Relatorios.Queries.TopClientes;

public sealed class TopClientesValidator : AbstractValidator<TopClientesQuery>
{
    public TopClientesValidator()
    {
        RuleFor(q => q.DataInicio)
            .NotNull()
            .WithErrorCode("periodo_obrigatorio")
            .WithMessage("Informe a data inicial do período.")
            .OverridePropertyName("dataInicio");

        RuleFor(q => q.DataFim)
            .NotNull()
            .WithErrorCode("periodo_obrigatorio")
            .WithMessage("Informe a data final do período.")
            .OverridePropertyName("dataFim");

        RuleFor(q => q)
            .Must(q => q.DataInicio is null || q.DataFim is null || q.DataInicio <= q.DataFim)
            .WithErrorCode("periodo_invertido")
            .WithMessage("A data inicial não pode ser posterior à data final.")
            .OverridePropertyName("dataInicio");
    }
}
