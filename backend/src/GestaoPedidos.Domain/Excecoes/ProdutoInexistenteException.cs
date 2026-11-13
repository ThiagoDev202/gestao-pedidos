namespace GestaoPedidos.Domain.Excecoes;

public sealed class ProdutoInexistenteException(int produtoId)
    : DomainException($"O produto {produtoId} não existe.")
{
    public int ProdutoId { get; } = produtoId;
}
