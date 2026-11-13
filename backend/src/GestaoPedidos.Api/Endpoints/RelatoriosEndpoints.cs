using FluentValidation;
using GestaoPedidos.Api.Contracts;
using GestaoPedidos.Application.Abstractions;
using GestaoPedidos.Application.Errors;
using GestaoPedidos.Application.Relatorios.Queries.TopClientes;

namespace GestaoPedidos.Api.Endpoints;

public static class RelatoriosEndpoints
{
    public static IEndpointRouteBuilder MapRelatorios(this IEndpointRouteBuilder rotas)
    {
        rotas.MapGroup("/relatorios").WithTags("Relatórios")
            .MapGet("/top-clientes", TopClientesAsync)
            .WithName("topClientes")
            .Produces<IReadOnlyList<TopClienteDto>>()
            .ComErros(400, 429, 500, 503);

        return rotas;
    }

    private static async Task<IResult> TopClientesAsync(
        [AsParameters] ParametrosDeRelatorio parametros,
        IQueryHandler<TopClientesQuery, IReadOnlyList<TopClienteDto>> handler,
        IValidator<TopClientesQuery> validator,
        CancellationToken cancellationToken)
    {
        var leitor = new ParametrosDeConsulta();
        var consulta = new TopClientesQuery(leitor.Data(parametros.DataInicio, "dataInicio"), leitor.Data(parametros.DataFim, "dataFim"));

        if (leitor.Erros.Count > 0)
        {
            var resultado = await validator.ValidateAsync(consulta, cancellationToken);
            throw new RequisicaoInvalidaException(leitor.Combinar(resultado.ParaErros()));
        }

        return TypedResults.Ok(await handler.HandleAsync(consulta, cancellationToken));
    }
}
