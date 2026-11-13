using GestaoPedidos.Application.Apoio.Dtos;

namespace GestaoPedidos.Application.Apoio.Ports;

public interface IApoioQueries
{
    Task<IReadOnlyList<ClienteDto>> ListarClientesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ProdutoDto>> ListarProdutosAsync(CancellationToken cancellationToken);
}
