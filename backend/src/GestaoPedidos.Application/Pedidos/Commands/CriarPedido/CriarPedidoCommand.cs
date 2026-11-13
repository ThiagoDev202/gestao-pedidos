namespace GestaoPedidos.Application.Pedidos.Commands.CriarPedido;

public sealed record ItemDoPedidoCommand(int ProdutoId, int Quantidade);

public sealed record CriarPedidoCommand(int ClienteId, IReadOnlyList<ItemDoPedidoCommand> Itens, Guid? ChaveIdempotencia);
