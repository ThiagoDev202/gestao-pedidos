using GestaoPedidos.Application.Apoio.Dtos;
using GestaoPedidos.Application.Apoio.Ports;

namespace GestaoPedidos.Infrastructure.Queries;

public sealed class ApoioQueries : IApoioQueries
{
    public Task<IReadOnlyList<ClienteDto>> ListarClientesAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<ProdutoDto>> ListarProdutosAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
