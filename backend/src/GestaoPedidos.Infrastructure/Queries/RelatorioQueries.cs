using GestaoPedidos.Application.Relatorios.Ports;
using GestaoPedidos.Application.Relatorios.Queries.TopClientes;

namespace GestaoPedidos.Infrastructure.Queries;

public sealed class RelatorioQueries : IRelatorioQueries
{
    public Task<IReadOnlyList<TopClienteDto>> TopClientesAsync(DateOnly inicio, DateOnly fim, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
