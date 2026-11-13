namespace GestaoPedidos.Domain.Pedidos;

public sealed record ItemParaPedido(int ProdutoId, int Quantidade, decimal PrecoUnitario);
