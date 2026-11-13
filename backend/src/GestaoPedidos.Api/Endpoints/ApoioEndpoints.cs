using GestaoPedidos.Application.Apoio.Dtos;
using GestaoPedidos.Application.Apoio.Ports;

namespace GestaoPedidos.Api.Endpoints;

public static class ApoioEndpoints
{
    public static IEndpointRouteBuilder MapApoio(this IEndpointRouteBuilder rotas)
    {
        rotas.MapGet("/clientes", async (IApoioQueries consultas, CancellationToken cancellationToken) =>
                TypedResults.Ok(await consultas.ListarClientesAsync(cancellationToken)))
            .WithTags("Clientes")
            .WithName("listarClientes")
            .Produces<IReadOnlyList<ClienteDto>>()
            .ComErros(429, 500, 503);

        rotas.MapGet("/produtos", async (IApoioQueries consultas, CancellationToken cancellationToken) =>
                TypedResults.Ok(await consultas.ListarProdutosAsync(cancellationToken)))
            .WithTags("Produtos")
            .WithName("listarProdutos")
            .Produces<IReadOnlyList<ProdutoDto>>()
            .ComErros(429, 500, 503);

        return rotas;
    }
}
