using FluentValidation.Results;

namespace GestaoPedidos.Application.Errors;

public static class ValidationResultExtensions
{
    public static IReadOnlyList<ErroDeCampo> ParaErros(this ValidationResult resultado) =>
        resultado.Errors
            .Select(falha => new ErroDeCampo(falha.PropertyName, falha.ErrorCode, falha.ErrorMessage))
            .ToList();
}
