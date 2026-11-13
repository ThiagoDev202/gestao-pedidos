using GestaoPedidos.Application.Pedidos.Dtos;

namespace GestaoPedidos.Application.Pedidos.Commands.CriarPedido;

public sealed record CriarPedidoResult(PedidoDetalheDto Pedido, bool Reenvio);
