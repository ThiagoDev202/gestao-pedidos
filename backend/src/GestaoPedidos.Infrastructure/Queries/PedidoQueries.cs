using System.Data;
using Dapper;
using GestaoPedidos.Application.Pedidos.Dtos;
using GestaoPedidos.Application.Pedidos.Ports;
using GestaoPedidos.Application.Pedidos.Queries.ListarPedidos;
using GestaoPedidos.Infrastructure.Resilience;

namespace GestaoPedidos.Infrastructure.Queries;

public sealed class PedidoQueries(ConsultaSql consulta) : IPedidoQueries
{
    private const string SqlCabecalho = """
        SELECT p.Id, p.ClienteId, c.Nome AS ClienteNome, p.DataPedido, p.ValorTotal
        FROM dbo.Pedidos AS p
        INNER JOIN dbo.Clientes AS c ON c.Id = p.ClienteId
        WHERE p.Id = @Id;
        """;

    private const string SqlItens = """
        SELECT i.ProdutoId, pr.Nome AS ProdutoNome, pr.Sku, i.Quantidade, i.PrecoUnitario, i.Quantidade * i.PrecoUnitario AS Subtotal
        FROM dbo.ItensPedido AS i
        INNER JOIN dbo.Produtos AS pr ON pr.Id = i.ProdutoId
        WHERE i.PedidoId = @Id
        ORDER BY i.ProdutoId;
        """;

    public Task<PedidoDetalheDto?> ObterAsync(int id, CancellationToken cancellationToken) =>
        consulta.ExecutarAsync(
            async (conexao, token) =>
            {
                using var lote = await conexao.QueryMultipleAsync(new CommandDefinition(SqlCabecalho + SqlItens, new { Id = id }, cancellationToken: token));
                var cabecalho = await lote.ReadFirstOrDefaultAsync<CabecalhoRow>();
                if (cabecalho is null)
                {
                    return null;
                }

                var itens = (await lote.ReadAsync<ItemRow>())
                    .Select(i => new ItemPedidoDto(i.ProdutoId, i.ProdutoNome, i.Sku, i.Quantidade, i.PrecoUnitario, i.Subtotal))
                    .ToList();

                return new PedidoDetalheDto(
                    cabecalho.Id,
                    cabecalho.ClienteId,
                    cabecalho.ClienteNome,
                    DateTime.SpecifyKind(cabecalho.DataPedido, DateTimeKind.Utc),
                    cabecalho.ValorTotal,
                    itens);
            },
            cancellationToken);

    public Task<PedidosPaginadosDto> ListarAsync(ListarPedidosQuery query, CancellationToken cancellationToken)
    {
        var filtros = new List<string>();
        var parametros = new DynamicParameters();

        if (query.ClienteId is { } clienteId)
        {
            filtros.Add("p.ClienteId = @ClienteId");
            parametros.Add("ClienteId", clienteId, DbType.Int32);
        }

        if (query.DataInicio is { } inicio)
        {
            filtros.Add("p.DataPedido >= @Inicio");
            parametros.Add("Inicio", inicio.ToDateTime(TimeOnly.MinValue), DbType.DateTime2);
        }

        if (query.DataFim is { } fim)
        {
            filtros.Add("p.DataPedido < @FimExclusivo");
            parametros.Add("FimExclusivo", fim.AddDays(1).ToDateTime(TimeOnly.MinValue), DbType.DateTime2);
        }

        var onde = filtros.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", filtros);
        var deslocamento = (long)(query.Page - 1) * query.PageSize;
        parametros.Add("Deslocamento", deslocamento, DbType.Int64);
        parametros.Add("TamanhoPagina", query.PageSize, DbType.Int32);

        var sql = $"""
            SELECT COUNT(*) FROM dbo.Pedidos AS p {onde};

            SELECT p.Id, p.ClienteId, c.Nome AS ClienteNome, p.DataPedido, p.ValorTotal,
                   (SELECT COUNT(*) FROM dbo.ItensPedido AS i WHERE i.PedidoId = p.Id) AS QuantidadeItens
            FROM dbo.Pedidos AS p
            INNER JOIN dbo.Clientes AS c ON c.Id = p.ClienteId
            {onde}
            ORDER BY p.DataPedido DESC, p.Id DESC
            OFFSET @Deslocamento ROWS FETCH NEXT @TamanhoPagina ROWS ONLY;
            """;

        return consulta.ExecutarAsync(
            async (conexao, token) =>
            {
                using var lote = await conexao.QueryMultipleAsync(new CommandDefinition(sql, parametros, cancellationToken: token));
                var total = await lote.ReadSingleAsync<int>();
                var linhas = (await lote.ReadAsync<ResumoRow>())
                    .Select(r => new PedidoResumoDto(
                        r.Id,
                        r.ClienteId,
                        r.ClienteNome,
                        DateTime.SpecifyKind(r.DataPedido, DateTimeKind.Utc),
                        r.ValorTotal,
                        r.QuantidadeItens))
                    .ToList();

                var totalDePaginas = (int)Math.Ceiling(total / (double)query.PageSize);
                return new PedidosPaginadosDto(linhas, query.Page, query.PageSize, total, totalDePaginas);
            },
            cancellationToken);
    }

    private sealed class ResumoRow
    {
        public int Id { get; set; }

        public int ClienteId { get; set; }

        public string ClienteNome { get; set; } = string.Empty;

        public DateTime DataPedido { get; set; }

        public decimal ValorTotal { get; set; }

        public int QuantidadeItens { get; set; }
    }

    private sealed class CabecalhoRow
    {
        public int Id { get; set; }

        public int ClienteId { get; set; }

        public string ClienteNome { get; set; } = string.Empty;

        public DateTime DataPedido { get; set; }

        public decimal ValorTotal { get; set; }
    }

    private sealed class ItemRow
    {
        public int ProdutoId { get; set; }

        public string ProdutoNome { get; set; } = string.Empty;

        public string Sku { get; set; } = string.Empty;

        public int Quantidade { get; set; }

        public decimal PrecoUnitario { get; set; }

        public decimal Subtotal { get; set; }
    }
}
