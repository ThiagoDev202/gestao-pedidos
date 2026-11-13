using GestaoPedidos.Application.Pedidos.Ports;
using Microsoft.EntityFrameworkCore;

namespace GestaoPedidos.Infrastructure.Persistence;

public sealed class ClienteRepository(GestaoPedidosDbContext contexto) : IClienteRepository
{
    public Task<bool> ExisteAsync(int clienteId, CancellationToken cancellationToken) =>
        contexto.Clientes.AnyAsync(c => c.Id == clienteId, cancellationToken);
}
