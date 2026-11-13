namespace GestaoPedidos.Domain.Excecoes;

public sealed class EstoqueInsuficienteException(int produtoId, string produtoNome, int solicitado, int disponivel)
    : DomainException($"O produto {produtoNome} tem {disponivel} unidade(s) em estoque e foram pedidas {solicitado}.")
{
    public int ProdutoId { get; } = produtoId;

    public string ProdutoNome { get; } = produtoNome;

    public int Solicitado { get; } = solicitado;

    public int Disponivel { get; } = disponivel;
}
