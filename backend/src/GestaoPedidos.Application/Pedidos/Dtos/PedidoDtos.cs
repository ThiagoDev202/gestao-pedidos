namespace GestaoPedidos.Application.Pedidos.Dtos;

public sealed record ItemPedidoDto(
    int ProdutoId,
    string ProdutoNome,
    string Sku,
    int Quantidade,
    decimal PrecoUnitario,
    decimal Subtotal);

public sealed record PedidoDetalheDto(
    int Id,
    int ClienteId,
    string ClienteNome,
    DateTime DataPedido,
    decimal ValorTotal,
    IReadOnlyList<ItemPedidoDto> Itens);

public sealed record PedidoResumoDto(
    int Id,
    int ClienteId,
    string ClienteNome,
    DateTime DataPedido,
    decimal ValorTotal,
    int QuantidadeItens);

public sealed record PedidosPaginadosDto(
    IReadOnlyList<PedidoResumoDto> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);
