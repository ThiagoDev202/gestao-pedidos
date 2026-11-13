namespace GestaoPedidos.Infrastructure.Persistence;

public sealed class RegistroIdempotenciaEntity
{
    public Guid Chave { get; set; }

    public required string HashCorpo { get; set; }

    public int PedidoId { get; set; }

    public DateTime CriadoEm { get; set; }
}
