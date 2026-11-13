using GestaoPedidos.Application.Abstractions;
using GestaoPedidos.Application.Pedidos.Dtos;
using GestaoPedidos.Application.Pedidos.Ports;

namespace GestaoPedidos.Application.Pedidos.Queries.ListarPedidos;

public sealed class ListarPedidosHandler(IPedidoQueries consultas) : IQueryHandler<ListarPedidosQuery, PedidosPaginadosDto>
{
    public Task<PedidosPaginadosDto> HandleAsync(ListarPedidosQuery query, CancellationToken cancellationToken) =>
        consultas.ListarAsync(query, cancellationToken);
}
