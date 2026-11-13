namespace GestaoPedidos.Domain.Excecoes;

public sealed class PedidoInvalidoException(string motivo) : DomainException(motivo);
