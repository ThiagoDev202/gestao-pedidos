using FluentValidation;
using GestaoPedidos.Api.Contracts;
using GestaoPedidos.Api.Errors;
using GestaoPedidos.Api.RateLimiting;
using GestaoPedidos.Application.Abstractions;
using GestaoPedidos.Application.Errors;
using GestaoPedidos.Application.Pedidos.Commands.CriarPedido;
using GestaoPedidos.Application.Pedidos.Dtos;

namespace GestaoPedidos.Api.Endpoints;

public static class PedidosEndpoints
{
    private const string CabecalhoIdempotencia = "Idempotency-Key";

    public static IEndpointRouteBuilder MapPedidos(this IEndpointRouteBuilder rotas)
    {
        var grupo = rotas.MapGroup("/pedidos").WithTags("Pedidos");

        grupo.MapPost("/", CriarAsync)
            .WithName("criarPedido")
            .RequireRateLimiting(RateLimitingExtensions.PoliticaCriarPedido)
            .Produces<PedidoDetalheDto>(StatusCodes.Status201Created)
            .Produces<PedidoDetalheDto>(StatusCodes.Status200OK)
            .Produces<ProblemaHttp>(StatusCodes.Status400BadRequest, "application/problem+json")
            .Produces<ProblemaHttp>(StatusCodes.Status422UnprocessableEntity, "application/problem+json");

        return rotas;
    }

    private static async Task<IResult> CriarAsync(
        CriarPedidoRequest requisicao,
        HttpContext contexto,
        ICommandHandler<CriarPedidoCommand, CriarPedidoResult> handler,
        IValidator<CriarPedidoCommand> validator,
        CancellationToken cancellationToken)
    {
        var erros = new List<ErroDeCampo>();
        Guid? chave = null;

        if (contexto.Request.Headers.TryGetValue(CabecalhoIdempotencia, out var valor) && !string.IsNullOrWhiteSpace(valor))
        {
            if (Guid.TryParse(valor.ToString(), out var parseada))
            {
                chave = parseada;
            }
            else
            {
                erros.Add(new ErroDeCampo(CabecalhoIdempotencia, "chave_idempotencia_invalida", "A chave de idempotência deve ser um UUID."));
            }
        }

        var comando = new CriarPedidoCommand(
            requisicao.ClienteId,
            (requisicao.Itens ?? []).Select(i => new ItemDoPedidoCommand(i.ProdutoId, i.Quantidade)).ToList(),
            chave);

        if (erros.Count > 0)
        {
            var resultadoDoCorpo = await validator.ValidateAsync(comando, cancellationToken);
            erros.AddRange(resultadoDoCorpo.ParaErros());
            throw new RequisicaoInvalidaException(erros);
        }

        var resultado = await handler.HandleAsync(comando, cancellationToken);
        var localizacao = $"/pedidos/{resultado.Pedido.Id}";

        if (resultado.Reenvio)
        {
            contexto.Response.Headers.Location = localizacao;
            return TypedResults.Ok(resultado.Pedido);
        }

        return TypedResults.Created(localizacao, resultado.Pedido);
    }
}
