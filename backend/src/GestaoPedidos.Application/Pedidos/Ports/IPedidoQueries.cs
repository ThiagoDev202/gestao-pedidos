using GestaoPedidos.Application.Pedidos.Dtos;
using GestaoPedidos.Application.Pedidos.Queries.ListarPedidos;

namespace GestaoPedidos.Application.Pedidos.Ports;

public interface IPedidoQueries
{
    Task<PedidoDetalheDto?> ObterAsync(int id, CancellationToken cancellationToken);

    Task<PedidosPaginadosDto> ListarAsync(ListarPedidosQuery query, CancellationToken cancellationToken);
}
