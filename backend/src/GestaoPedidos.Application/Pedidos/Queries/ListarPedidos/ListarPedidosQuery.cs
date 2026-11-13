namespace GestaoPedidos.Application.Pedidos.Queries.ListarPedidos;

public sealed record ListarPedidosQuery(int? ClienteId, DateOnly? DataInicio, DateOnly? DataFim, int Page, int PageSize);
