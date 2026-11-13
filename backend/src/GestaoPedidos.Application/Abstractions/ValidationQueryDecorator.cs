using FluentValidation;
using GestaoPedidos.Application.Errors;

namespace GestaoPedidos.Application.Abstractions;

public sealed class ValidationQueryDecorator<TQuery, TResult>(
    IQueryHandler<TQuery, TResult> inner,
    IEnumerable<IValidator<TQuery>> validators) : IQueryHandler<TQuery, TResult>
{
    public async Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken)
    {
        var erros = new List<ErroDeCampo>();
        foreach (var validator in validators)
        {
            var resultado = await validator.ValidateAsync(query, cancellationToken);
            erros.AddRange(resultado.ParaErros());
        }

        if (erros.Count > 0)
        {
            throw new RequisicaoInvalidaException(erros);
        }

        return await inner.HandleAsync(query, cancellationToken);
    }
}
