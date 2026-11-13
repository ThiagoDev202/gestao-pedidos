using GestaoPedidos.Application.Abstractions;
using GestaoPedidos.Application.Relatorios.Ports;

namespace GestaoPedidos.Application.Relatorios.Queries.TopClientes;

public sealed class TopClientesHandler(IRelatorioQueries consultas) : IQueryHandler<TopClientesQuery, IReadOnlyList<TopClienteDto>>
{
    public Task<IReadOnlyList<TopClienteDto>> HandleAsync(TopClientesQuery query, CancellationToken cancellationToken) =>
        consultas.TopClientesAsync(query.DataInicio!.Value, query.DataFim!.Value, cancellationToken);
}
