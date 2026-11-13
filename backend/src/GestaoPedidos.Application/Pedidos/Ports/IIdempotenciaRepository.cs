namespace GestaoPedidos.Application.Pedidos.Ports;

public sealed record RegistroIdempotencia(Guid Chave, string HashCorpo, int PedidoId);

public interface IIdempotenciaRepository
{
    Task AdquirirTravaAsync(Guid chave, CancellationToken cancellationToken);

    Task<RegistroIdempotencia?> ObterAsync(Guid chave, CancellationToken cancellationToken);

    Task RegistrarAsync(Guid chave, string hashCorpo, int pedidoId, CancellationToken cancellationToken);
}
