using GestaoPedidos.Application.Pedidos.Ports;
using GestaoPedidos.Domain.Pedidos;

namespace GestaoPedidos.Infrastructure.Persistence;

public sealed class PedidoRepository(GestaoPedidosDbContext contexto) : IPedidoRepository
{
    public async Task AdicionarAsync(Pedido pedido, CancellationToken cancellationToken)
    {
        contexto.Pedidos.Add(pedido);
        await contexto.SaveChangesAsync(cancellationToken);
    }
}
