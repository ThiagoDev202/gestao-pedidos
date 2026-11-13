namespace GestaoPedidos.Domain.Excecoes;

public sealed class PedidoInvalidoException(string motivo, string campo = "itens", string codigo = "itens_vazio") : DomainException(motivo)
{
    public string Campo { get; } = campo;

    public string Codigo { get; } = codigo;
}
