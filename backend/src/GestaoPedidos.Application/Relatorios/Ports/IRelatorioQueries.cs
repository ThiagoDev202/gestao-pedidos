using GestaoPedidos.Application.Relatorios.Queries.TopClientes;

namespace GestaoPedidos.Application.Relatorios.Ports;

public interface IRelatorioQueries
{
    Task<IReadOnlyList<TopClienteDto>> TopClientesAsync(DateOnly inicio, DateOnly fim, CancellationToken cancellationToken);
}
