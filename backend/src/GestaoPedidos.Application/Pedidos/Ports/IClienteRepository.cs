namespace GestaoPedidos.Application.Pedidos.Ports;

public interface IClienteRepository
{
    Task<bool> ExisteAsync(int clienteId, CancellationToken cancellationToken);
}
