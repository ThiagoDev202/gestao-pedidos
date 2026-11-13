namespace GestaoPedidos.Application.Errors;

public sealed class RequisicaoInvalidaException(IReadOnlyList<ErroDeCampo> erros)
    : Exception("A requisição é inválida.")
{
    public IReadOnlyList<ErroDeCampo> Erros { get; } = erros;
}
