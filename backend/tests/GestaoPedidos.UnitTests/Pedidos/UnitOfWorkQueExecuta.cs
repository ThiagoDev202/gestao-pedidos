using GestaoPedidos.Application.Abstractions;

namespace GestaoPedidos.UnitTests.Pedidos;

public sealed class UnitOfWorkQueExecuta : IUnitOfWork
{
    public int Execucoes { get; private set; }

    public bool EmExecucao { get; private set; }

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken)
    {
        Execucoes++;
        EmExecucao = true;
        try
        {
            return await work(cancellationToken);
        }
        finally
        {
            EmExecucao = false;
        }
    }
}
