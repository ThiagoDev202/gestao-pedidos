namespace GestaoPedidos.Api.Contracts;

public sealed record ItemRequest(int ProdutoId, int Quantidade);

public sealed record CriarPedidoRequest(int ClienteId, List<ItemRequest>? Itens);
