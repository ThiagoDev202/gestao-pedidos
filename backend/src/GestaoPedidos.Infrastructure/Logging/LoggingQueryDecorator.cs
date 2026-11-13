using System.Diagnostics;
using GestaoPedidos.Application.Abstractions;
using GestaoPedidos.Domain.Excecoes;
using Microsoft.Extensions.Logging;

namespace GestaoPedidos.Infrastructure.Logging;

public sealed class LoggingQueryDecorator<TQuery, TResult>(
    IQueryHandler<TQuery, TResult> inner,
    ILogger<LoggingQueryDecorator<TQuery, TResult>> logger) : IQueryHandler<TQuery, TResult>
{
    public async Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken)
    {
        var inicio = Stopwatch.GetTimestamp();
        try
        {
            var resultado = await inner.HandleAsync(query, cancellationToken);
            var duracao = Stopwatch.GetElapsedTime(inicio).TotalMilliseconds;
            LogMessages.Concluido(logger, "Consulta", typeof(TQuery).Name, duracao);
            return resultado;
        }
        catch (DomainException exception)
        {
            LogMessages.Recusado(logger, "Consulta", typeof(TQuery).Name, exception.Message);
            throw;
        }
        catch (Exception exception)
        {
            LogMessages.Falhou(logger, exception, "Consulta", typeof(TQuery).Name);
            throw;
        }
    }
}
