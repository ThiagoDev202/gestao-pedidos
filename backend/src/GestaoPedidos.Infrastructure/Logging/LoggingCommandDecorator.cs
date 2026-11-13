using System.Diagnostics;
using GestaoPedidos.Application.Abstractions;
using GestaoPedidos.Domain.Excecoes;
using Microsoft.Extensions.Logging;

namespace GestaoPedidos.Infrastructure.Logging;

public sealed class LoggingCommandDecorator<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    ILogger<LoggingCommandDecorator<TCommand, TResult>> logger) : ICommandHandler<TCommand, TResult>
{
    public async Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        var inicio = Stopwatch.GetTimestamp();
        try
        {
            var resultado = await inner.HandleAsync(command, cancellationToken);
            var duracao = Stopwatch.GetElapsedTime(inicio).TotalMilliseconds;
            LogMessages.Concluido(logger, "Comando", typeof(TCommand).Name, duracao);
            return resultado;
        }
        catch (DomainException exception)
        {
            LogMessages.Recusado(logger, "Comando", typeof(TCommand).Name, exception.Message);
            throw;
        }
        catch (Exception exception)
        {
            LogMessages.Falhou(logger, exception, "Comando", typeof(TCommand).Name);
            throw;
        }
    }
}
