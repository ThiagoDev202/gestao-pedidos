namespace GestaoPedidos.Application.Apoio.Dtos;

public sealed record ClienteDto(int Id, string Nome, string Email);

public sealed record ProdutoDto(int Id, string Nome, string Sku, decimal Preco, int Estoque);
