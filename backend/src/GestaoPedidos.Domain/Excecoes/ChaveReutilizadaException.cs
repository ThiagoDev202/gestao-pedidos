namespace GestaoPedidos.Domain.Excecoes;

public sealed class ChaveReutilizadaException(Guid chave)
    : DomainException($"A chave de idempotência {chave} já foi usada com um pedido diferente.")
{
    public Guid Chave { get; } = chave;
}
