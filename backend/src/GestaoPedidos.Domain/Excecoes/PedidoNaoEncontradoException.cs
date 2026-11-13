namespace GestaoPedidos.Domain.Excecoes;

public sealed class PedidoNaoEncontradoException(int pedidoId)
    : DomainException($"O pedido {pedidoId} não foi encontrado.")
{
    public int PedidoId { get; } = pedidoId;
}
