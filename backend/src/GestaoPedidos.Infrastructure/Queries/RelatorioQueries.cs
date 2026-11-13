using System.Data;
using Dapper;
using GestaoPedidos.Application.Relatorios.Ports;
using GestaoPedidos.Application.Relatorios.Queries.TopClientes;
using GestaoPedidos.Infrastructure.Resilience;

namespace GestaoPedidos.Infrastructure.Queries;

public sealed class RelatorioQueries(ConsultaSql consulta) : IRelatorioQueries
{
    private const string Sql = """
        SELECT TOP (5) c.Id AS ClienteId, c.Nome, SUM(p.ValorTotal) AS ValorTotal, COUNT(*) AS QuantidadePedidos
        FROM dbo.Pedidos AS p
        INNER JOIN dbo.Clientes AS c ON c.Id = p.ClienteId
        WHERE p.DataPedido >= @Inicio AND p.DataPedido < @FimExclusivo
        GROUP BY c.Id, c.Nome
        ORDER BY SUM(p.ValorTotal) DESC, c.Nome, c.Id;
        """;

    public Task<IReadOnlyList<TopClienteDto>> TopClientesAsync(DateOnly inicio, DateOnly fim, CancellationToken cancellationToken)
    {
        var parametros = new DynamicParameters();
        parametros.Add("Inicio", inicio.ToDateTime(TimeOnly.MinValue), DbType.DateTime2);
        parametros.Add("FimExclusivo", fim.AddDays(1).ToDateTime(TimeOnly.MinValue), DbType.DateTime2);

        return consulta.ExecutarAsync<IReadOnlyList<TopClienteDto>>(
            async (conexao, token) =>
            {
                var linhas = await conexao.QueryAsync<LinhaRanking>(new CommandDefinition(Sql, parametros, cancellationToken: token));
                return linhas.Select(l => new TopClienteDto(l.ClienteId, l.Nome, l.ValorTotal, l.QuantidadePedidos)).ToList();
            },
            cancellationToken);
    }

    private sealed class LinhaRanking
    {
        public int ClienteId { get; set; }

        public string Nome { get; set; } = string.Empty;

        public decimal ValorTotal { get; set; }

        public int QuantidadePedidos { get; set; }
    }
}
