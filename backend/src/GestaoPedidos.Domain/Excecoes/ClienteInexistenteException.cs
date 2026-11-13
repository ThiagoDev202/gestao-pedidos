namespace GestaoPedidos.Domain.Excecoes;

public sealed class ClienteInexistenteException(int clienteId)
    : DomainException($"O cliente {clienteId} não existe.")
{
    public int ClienteId { get; } = clienteId;
}
