using GestaoPedidos.Application.Pedidos.Ports;
using Microsoft.EntityFrameworkCore;

namespace GestaoPedidos.Infrastructure.Persistence;

public sealed class IdempotenciaRepository(GestaoPedidosDbContext contexto) : IIdempotenciaRepository
{
    public async Task AdquirirTravaAsync(Guid chave, CancellationToken cancellationToken)
    {
        var recurso = chave.ToString();
        await contexto.Database.ExecuteSqlInterpolatedAsync(
            $"EXEC sp_getapplock @Resource = {recurso}, @LockMode = N'Exclusive', @LockOwner = N'Transaction', @LockTimeout = 30000",
            cancellationToken);
    }

    public async Task<RegistroIdempotencia?> ObterAsync(Guid chave, CancellationToken cancellationToken)
    {
        var registro = await contexto.Idempotencias.AsNoTracking().FirstOrDefaultAsync(r => r.Chave == chave, cancellationToken);
        return registro is null ? null : new RegistroIdempotencia(registro.Chave, registro.HashCorpo, registro.PedidoId);
    }

    public async Task RegistrarAsync(Guid chave, string hashCorpo, int pedidoId, CancellationToken cancellationToken)
    {
        contexto.Idempotencias.Add(new RegistroIdempotenciaEntity { Chave = chave, HashCorpo = hashCorpo, PedidoId = pedidoId });
        await contexto.SaveChangesAsync(cancellationToken);
    }
}
