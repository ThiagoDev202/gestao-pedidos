using GestaoPedidos.Domain.Produtos;

namespace GestaoPedidos.Application.Pedidos.Ports;

public interface IProdutoRepository
{
    Task<IReadOnlyList<Produto>> ObterPorIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken);

    Task<bool> TentarBaixarEstoqueAsync(int produtoId, int quantidade, CancellationToken cancellationToken);
}
