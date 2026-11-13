using FluentValidation;
using GestaoPedidos.Api.Contracts;
using GestaoPedidos.Api.RateLimiting;
using GestaoPedidos.Application.Abstractions;
using GestaoPedidos.Application.Errors;
using GestaoPedidos.Application.Pedidos.Commands.CriarPedido;
using GestaoPedidos.Application.Pedidos.Dtos;
using GestaoPedidos.Application.Pedidos.Queries.ListarPedidos;
using GestaoPedidos.Application.Pedidos.Queries.ObterPedido;
using Microsoft.AspNetCore.Mvc;

namespace GestaoPedidos.Api.Endpoints;

public static class PedidosEndpoints
{
    public static IEndpointRouteBuilder MapPedidos(this IEndpointRouteBuilder rotas)
    {
        var grupo = rotas.MapGroup("/pedidos").WithTags("Pedidos");

        grupo.MapPost("/", CriarAsync)
            .WithName("criarPedido")
            .RequireRateLimiting(RateLimitingExtensions.PoliticaCriarPedido)
            .Produces<PedidoDetalheDto>(StatusCodes.Status201Created)
            .Produces<PedidoDetalheDto>(StatusCodes.Status200OK)
            .ComErros(400, 415, 422, 429, 500, 503);

        grupo.MapGet("/", ListarAsync)
            .WithName("listarPedidos")
            .Produces<PedidosPaginadosDto>()
            .ComErros(400, 429, 500, 503);

        grupo.MapGet("/{id:int}", ObterAsync)
            .WithName("obterPedido")
            .Produces<PedidoDetalheDto>()
            .ComErros(404, 429, 500, 503);

        return rotas;
    }

    private static async Task<IResult> CriarAsync(
        CriarPedidoRequest requisicao,
        HttpContext contexto,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        ICommandHandler<CriarPedidoCommand, CriarPedidoResult> handler,
        IValidator<CriarPedidoCommand> validator,
        CancellationToken cancellationToken)
    {
        var erros = new List<ErroDeCampo>();
        Guid? chave = null;

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            if (Guid.TryParse(idempotencyKey, out var parseada))
            {
                chave = parseada;
            }
            else
            {
                erros.Add(new ErroDeCampo("Idempotency-Key", "chave_idempotencia_invalida", "A chave de idempotência deve ser um UUID."));
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

    private static async Task<IResult> ListarAsync(
        [AsParameters] ParametrosDeListagem parametros,
        IQueryHandler<ListarPedidosQuery, PedidosPaginadosDto> handler,
        IValidator<ListarPedidosQuery> validator,
        CancellationToken cancellationToken)
    {
        var leitor = new ParametrosDeConsulta();
        var consulta = new ListarPedidosQuery(
            leitor.InteiroOpcional(parametros.ClienteId, "clienteId", "cliente_id_invalido", "O cliente informado é inválido."),
            leitor.Data(parametros.DataInicio, "dataInicio"),
            leitor.Data(parametros.DataFim, "dataFim"),
            leitor.Inteiro(parametros.Page, 1, "page", "pagina_invalida", "A página deve ser um número inteiro maior ou igual a 1."),
            leitor.Inteiro(parametros.PageSize, 20, "pageSize", "tamanho_pagina_invalido", "O tamanho da página deve ser um número inteiro entre 1 e 100."));

        if (leitor.Erros.Count > 0)
        {
            var resultado = await validator.ValidateAsync(consulta, cancellationToken);
            throw new RequisicaoInvalidaException(leitor.Combinar(resultado.ParaErros()));
        }

        return TypedResults.Ok(await handler.HandleAsync(consulta, cancellationToken));
    }

    private static async Task<IResult> ObterAsync(
        int id,
        IQueryHandler<ObterPedidoQuery, PedidoDetalheDto> handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HandleAsync(new ObterPedidoQuery(id), cancellationToken));
}
