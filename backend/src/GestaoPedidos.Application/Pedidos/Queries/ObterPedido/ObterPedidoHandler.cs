#pragma warning disable CS9113

using GestaoPedidos.Application.Abstractions;
using GestaoPedidos.Application.Pedidos.Dtos;
using GestaoPedidos.Application.Pedidos.Ports;

namespace GestaoPedidos.Application.Pedidos.Queries.ObterPedido;

public sealed class ObterPedidoHandler(IPedidoQueries consultas) : IQueryHandler<ObterPedidoQuery, PedidoDetalheDto>
{
    public Task<PedidoDetalheDto> HandleAsync(ObterPedidoQuery query, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
