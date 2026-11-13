using FluentValidation;
using GestaoPedidos.Application.Errors;

namespace GestaoPedidos.Application.Abstractions;

public sealed class ValidationCommandDecorator<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    IEnumerable<IValidator<TCommand>> validators) : ICommandHandler<TCommand, TResult>
{
    public async Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        var erros = new List<ErroDeCampo>();
        foreach (var validator in validators)
        {
            var resultado = await validator.ValidateAsync(command, cancellationToken);
            erros.AddRange(resultado.ParaErros());
        }

        if (erros.Count > 0)
        {
            throw new RequisicaoInvalidaException(erros);
        }

        return await inner.HandleAsync(command, cancellationToken);
    }
}
