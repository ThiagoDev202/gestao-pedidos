using GestaoPedidos.Application.Abstractions;
using GestaoPedidos.Application.Pedidos.Dtos;
using GestaoPedidos.Application.Pedidos.Ports;
using GestaoPedidos.Domain.Excecoes;

namespace GestaoPedidos.Application.Pedidos.Queries.ObterPedido;

public sealed class ObterPedidoHandler(IPedidoQueries consultas) : IQueryHandler<ObterPedidoQuery, PedidoDetalheDto>
{
    public async Task<PedidoDetalheDto> HandleAsync(ObterPedidoQuery query, CancellationToken cancellationToken) =>
        await consultas.ObterAsync(query.Id, cancellationToken) ?? throw new PedidoNaoEncontradoException(query.Id);
}
