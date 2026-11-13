namespace GestaoPedidos.Domain.Produtos;

public sealed class Produto
{
    public int Id { get; init; }

    public required string Nome { get; init; }

    public required string Sku { get; init; }

    public decimal Preco { get; init; }

    public int Estoque { get; init; }
}
