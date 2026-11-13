#pragma warning disable CS9113

using GestaoPedidos.Application.Abstractions;
using GestaoPedidos.Application.Pedidos.Ports;

namespace GestaoPedidos.Application.Pedidos.Commands.CriarPedido;

public sealed class CriarPedidoHandler(
    IClienteRepository clientes,
    IProdutoRepository produtos,
    IPedidoRepository pedidos,
    IIdempotenciaRepository idempotencia,
    IPedidoQueries consultas,
    IUnitOfWork unitOfWork,
    IClock clock) : ICommandHandler<CriarPedidoCommand, CriarPedidoResult>
{
    public Task<CriarPedidoResult> HandleAsync(CriarPedidoCommand command, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
