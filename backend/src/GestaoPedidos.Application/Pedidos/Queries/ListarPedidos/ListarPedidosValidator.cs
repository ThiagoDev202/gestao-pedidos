using FluentValidation;

namespace GestaoPedidos.Application.Pedidos.Queries.ListarPedidos;

public sealed class ListarPedidosValidator : AbstractValidator<ListarPedidosQuery>
{
    public const int TamanhoMaximoDePagina = 100;

    public ListarPedidosValidator()
    {
        RuleFor(q => q.Page)
            .GreaterThanOrEqualTo(1)
            .WithErrorCode("pagina_invalida")
            .WithMessage("A página deve ser maior ou igual a 1.")
            .OverridePropertyName("page");

        RuleFor(q => q.PageSize)
            .InclusiveBetween(1, TamanhoMaximoDePagina)
            .WithErrorCode("tamanho_pagina_invalido")
            .WithMessage($"O tamanho da página deve estar entre 1 e {TamanhoMaximoDePagina}.")
            .OverridePropertyName("pageSize");

        RuleFor(q => q.ClienteId)
            .GreaterThan(0)
            .When(q => q.ClienteId.HasValue)
            .WithErrorCode("cliente_id_invalido")
            .WithMessage("O cliente informado é inválido.")
            .OverridePropertyName("clienteId");

        RuleFor(q => q)
            .Must(q => q.DataInicio is null || q.DataFim is null || q.DataInicio <= q.DataFim)
            .WithErrorCode("periodo_invertido")
            .WithMessage("A data inicial não pode ser posterior à data final.")
            .OverridePropertyName("dataInicio");
    }
}
