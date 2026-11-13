using GestaoPedidos.Domain.Pedidos;

namespace GestaoPedidos.Application.Pedidos.Ports;

public interface IPedidoRepository
{
    Task AdicionarAsync(Pedido pedido, CancellationToken cancellationToken);
}
