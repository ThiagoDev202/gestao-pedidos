using GestaoPedidos.Application.Abstractions;
using GestaoPedidos.Application.Errors;
using Microsoft.EntityFrameworkCore;

namespace GestaoPedidos.Infrastructure.Persistence;

public sealed class UnitOfWork(GestaoPedidosDbContext contexto) : IUnitOfWork
{
    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken)
    {
        var estrategia = contexto.Database.CreateExecutionStrategy();

        try
        {
            return await estrategia.ExecuteAsync(
                async token =>
                {
                    contexto.ChangeTracker.Clear();
                    await using var transacao = await contexto.Database.BeginTransactionAsync(token);
                    var resultado = await work(token);
                    await transacao.CommitAsync(token);
                    return resultado;
                },
                cancellationToken);
        }
        catch (Exception exception) when (ErrosDeBanco.EhIndisponibilidade(exception))
        {
            throw new BancoIndisponivelException(exception);
        }
    }
}
