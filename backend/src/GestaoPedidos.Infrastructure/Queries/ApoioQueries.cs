using Dapper;
using GestaoPedidos.Application.Apoio.Dtos;
using GestaoPedidos.Application.Apoio.Ports;
using GestaoPedidos.Infrastructure.Resilience;

namespace GestaoPedidos.Infrastructure.Queries;

public sealed class ApoioQueries(ConsultaSql consulta) : IApoioQueries
{
    public Task<IReadOnlyList<ClienteDto>> ListarClientesAsync(CancellationToken cancellationToken) =>
        consulta.ExecutarAsync<IReadOnlyList<ClienteDto>>(
            async (conexao, token) =>
            {
                var linhas = await conexao.QueryAsync<ClienteRow>(new CommandDefinition(
                    "SELECT Id, Nome, Email FROM dbo.Clientes ORDER BY Nome, Id",
                    cancellationToken: token));
                return linhas.Select(l => new ClienteDto(l.Id, l.Nome, l.Email)).ToList();
            },
            cancellationToken);

    public Task<IReadOnlyList<ProdutoDto>> ListarProdutosAsync(CancellationToken cancellationToken) =>
        consulta.ExecutarAsync<IReadOnlyList<ProdutoDto>>(
            async (conexao, token) =>
            {
                var linhas = await conexao.QueryAsync<ProdutoRow>(new CommandDefinition(
                    "SELECT Id, Nome, Sku, Preco, Estoque FROM dbo.Produtos ORDER BY Nome, Id",
                    cancellationToken: token));
                return linhas.Select(l => new ProdutoDto(l.Id, l.Nome, l.Sku, l.Preco, l.Estoque)).ToList();
            },
            cancellationToken);

    private sealed class ClienteRow
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
    }

    private sealed class ProdutoRow
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string Sku { get; set; } = string.Empty;

        public decimal Preco { get; set; }

        public int Estoque { get; set; }
    }
}
