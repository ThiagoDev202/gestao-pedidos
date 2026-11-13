using FluentValidation;

namespace GestaoPedidos.Application.Pedidos.Commands.CriarPedido;

public sealed class CriarPedidoValidator : AbstractValidator<CriarPedidoCommand>
{
    public const int MaximoDeItens = 50;

    public CriarPedidoValidator()
    {
        RuleFor(c => c.ClienteId)
            .GreaterThan(0)
            .WithErrorCode("cliente_id_invalido")
            .WithMessage("O cliente informado é inválido.")
            .OverridePropertyName("clienteId");

        RuleFor(c => c.Itens)
            .NotEmpty()
            .WithErrorCode("itens_vazio")
            .WithMessage("O pedido precisa de ao menos um item.")
            .OverridePropertyName("itens");

        RuleFor(c => c.Itens)
            .Must(itens => itens.Count <= MaximoDeItens)
            .When(c => c.Itens is not null)
            .WithErrorCode("itens_excedem_limite")
            .WithMessage($"O pedido aceita no máximo {MaximoDeItens} itens.")
            .OverridePropertyName("itens");

        RuleFor(c => c.Itens)
            .Custom((itens, contexto) =>
            {
                if (itens is null)
                {
                    return;
                }

                var vistos = new HashSet<int>();
                for (var i = 0; i < itens.Count; i++)
                {
                    var item = itens[i];
                    if (item.ProdutoId <= 0)
                    {
                        contexto.AddFailure(new FluentValidation.Results.ValidationFailure($"itens[{i}].produtoId", "O produto informado é inválido.") { ErrorCode = "produto_id_invalido" });
                    }
                    else if (!vistos.Add(item.ProdutoId))
                    {
                        contexto.AddFailure(new FluentValidation.Results.ValidationFailure($"itens[{i}].produtoId", $"O produto {item.ProdutoId} aparece mais de uma vez no pedido.") { ErrorCode = "produto_repetido" });
                    }

                    if (item.Quantidade <= 0)
                    {
                        contexto.AddFailure(new FluentValidation.Results.ValidationFailure($"itens[{i}].quantidade", "A quantidade deve ser maior que zero.") { ErrorCode = "quantidade_invalida" });
                    }
                }
            });
    }
}
