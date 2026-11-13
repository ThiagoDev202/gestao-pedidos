using GestaoPedidos.Application.Pedidos.Ports;
using GestaoPedidos.Domain.Produtos;
using Microsoft.EntityFrameworkCore;

namespace GestaoPedidos.Infrastructure.Persistence;

public sealed class ProdutoRepository(GestaoPedidosDbContext contexto) : IProdutoRepository
{
    public async Task<IReadOnlyList<Produto>> ObterPorIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken) =>
        await contexto.Produtos.AsNoTracking().Where(p => ids.Contains(p.Id)).ToListAsync(cancellationToken);

    public async Task<bool> TentarBaixarEstoqueAsync(int produtoId, int quantidade, CancellationToken cancellationToken)
    {
        var afetadas = await contexto.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE dbo.Produtos SET Estoque = Estoque - {quantidade} WHERE Id = {produtoId} AND Estoque >= {quantidade}",
            cancellationToken);
        return afetadas > 0;
    }
}
