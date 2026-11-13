using GestaoPedidos.Api.Errors;
using GestaoPedidos.Application.Errors;
using GestaoPedidos.Domain.Excecoes;
using Shouldly;

namespace GestaoPedidos.UnitTests.Errors;

public sealed class ErrorCatalogTests
{
    private static readonly string[] CodesDaPolitica =
    [
        "validacao_falhou",
        "pedido_nao_encontrado",
        "recurso_nao_encontrado",
        "formato_nao_suportado",
        "cliente_inexistente",
        "produto_inexistente",
        "estoque_insuficiente",
        "chave_reutilizada",
        "limite_requisicoes_excedido",
        "erro_interno",
        "banco_indisponivel",
    ];

    public static TheoryData<Exception> ExcecoesConhecidas => new()
    {
        new ClienteInexistenteException(15),
        new ProdutoInexistenteException(8),
        new EstoqueInsuficienteException(12, "Teclado Mecânico", 5, 2),
        new ChaveReutilizadaException(Guid.Parse("6f1c2a7e-3a4b-4c5d-8e9f-0a1b2c3d4e5f")),
        new PedidoNaoEncontradoException(9),
        new PedidoInvalidoException("O pedido precisa ter ao menos um item."),
        new RequisicaoInvalidaException([new ErroDeCampo("clienteId", "cliente_id_invalido", "Informe o cliente.")]),
        new InvalidOperationException("falha qualquer"),
    };

    [Fact]
    public void Lists_exactly_the_codes_of_the_error_policy()
    {
        ErrorCatalog.TodosOsCodes.ShouldBe(CodesDaPolitica, ignoreOrder: true);
    }

    [Theory]
    [MemberData(nameof(ExcecoesConhecidas))]
    public void Maps_every_exception_to_a_complete_problem_with_a_catalog_code(Exception excecao)
    {
        var problema = ErrorCatalog.Mapear(excecao);

        ErrorCatalog.TodosOsCodes.ShouldContain(problema.Code);
        problema.Type.ShouldBe("urn:gestaopedidos:erro:" + problema.Code);
        problema.Title.ShouldNotBeNullOrWhiteSpace();
        problema.Detail.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Maps_missing_customer_to_422_with_customer_id()
    {
        var problema = ErrorCatalog.Mapear(new ClienteInexistenteException(15));

        problema.Status.ShouldBe(422);
        problema.Code.ShouldBe("cliente_inexistente");
        problema.Type.ShouldBe("urn:gestaopedidos:erro:cliente_inexistente");
        problema.Meta.ShouldNotBeNull();
        problema.Meta["clienteId"].ShouldBe(15);
    }

    [Fact]
    public void Maps_missing_product_to_422_with_product_id()
    {
        var problema = ErrorCatalog.Mapear(new ProdutoInexistenteException(8));

        problema.Status.ShouldBe(422);
        problema.Code.ShouldBe("produto_inexistente");
        problema.Type.ShouldBe("urn:gestaopedidos:erro:produto_inexistente");
        problema.Meta.ShouldNotBeNull();
        problema.Meta["produtoId"].ShouldBe(8);
    }

    [Fact]
    public void Maps_insufficient_stock_to_422_with_product_requested_and_available()
    {
        var problema = ErrorCatalog.Mapear(new EstoqueInsuficienteException(12, "Teclado Mecânico", 5, 2));

        problema.Status.ShouldBe(422);
        problema.Code.ShouldBe("estoque_insuficiente");
        problema.Type.ShouldBe("urn:gestaopedidos:erro:estoque_insuficiente");
        problema.Detail.ShouldContain("Teclado Mecânico");
        problema.Detail.ShouldContain("5");
        problema.Detail.ShouldContain("2");
        problema.Meta.ShouldNotBeNull();
        problema.Meta["produtoId"].ShouldBe(12);
        problema.Meta["produtoNome"].ShouldBe("Teclado Mecânico");
        problema.Meta["solicitado"].ShouldBe(5);
        problema.Meta["disponivel"].ShouldBe(2);
    }

    [Fact]
    public void Maps_reused_idempotency_key_to_422()
    {
        var problema = ErrorCatalog.Mapear(new ChaveReutilizadaException(Guid.NewGuid()));

        problema.Status.ShouldBe(422);
        problema.Code.ShouldBe("chave_reutilizada");
        problema.Type.ShouldBe("urn:gestaopedidos:erro:chave_reutilizada");
    }

    [Fact]
    public void Maps_missing_order_to_404_with_order_id()
    {
        var problema = ErrorCatalog.Mapear(new PedidoNaoEncontradoException(9));

        problema.Status.ShouldBe(404);
        problema.Code.ShouldBe("pedido_nao_encontrado");
        problema.Type.ShouldBe("urn:gestaopedidos:erro:pedido_nao_encontrado");
        problema.Meta.ShouldNotBeNull();
        problema.Meta["pedidoId"].ShouldBe(9);
    }

    [Fact]
    public void Maps_invalid_order_from_domain_to_400_with_field_errors()
    {
        var problema = ErrorCatalog.Mapear(new PedidoInvalidoException("O pedido precisa ter ao menos um item."));

        problema.Status.ShouldBe(400);
        problema.Code.ShouldBe("validacao_falhou");
        problema.Type.ShouldBe("urn:gestaopedidos:erro:validacao_falhou");
        problema.Errors.ShouldNotBeNull();
        problema.Errors.ShouldNotBeEmpty();
    }

    [Fact]
    public void Maps_invalid_request_to_400_keeping_every_field_error()
    {
        ErroDeCampo[] erros =
        [
            new("clienteId", "cliente_id_invalido", "Informe um cliente válido."),
            new("itens[0].quantidade", "quantidade_invalida", "A quantidade deve ser maior que zero."),
            new("Idempotency-Key", "chave_idempotencia_invalida", "A chave deve ser um UUID."),
        ];

        var problema = ErrorCatalog.Mapear(new RequisicaoInvalidaException(erros));

        problema.Status.ShouldBe(400);
        problema.Code.ShouldBe("validacao_falhou");
        problema.Type.ShouldBe("urn:gestaopedidos:erro:validacao_falhou");
        problema.Errors.ShouldNotBeNull();
        problema.Errors.ShouldBe(erros);
    }

    [Fact]
    public void Maps_unexpected_exception_to_500_without_leaking_its_message()
    {
        const string MensagemInterna = "Invalid object name 'dbo.Produtos' em SqlConnection 10.1.2.3";

        var problema = ErrorCatalog.Mapear(new InvalidOperationException(MensagemInterna));

        problema.Status.ShouldBe(500);
        problema.Code.ShouldBe("erro_interno");
        problema.Type.ShouldBe("urn:gestaopedidos:erro:erro_interno");
        problema.Detail.ShouldNotBeNullOrWhiteSpace();
        problema.Detail.ShouldNotContain(MensagemInterna);
        problema.Detail.ShouldNotContain("dbo.Produtos");
        problema.Detail.ShouldNotContain("InvalidOperationException");
        problema.Title.ShouldNotContain(MensagemInterna);
    }

    [Fact]
    public void Uses_the_same_title_and_a_specific_detail_for_each_occurrence_of_a_code()
    {
        var primeiro = ErrorCatalog.Mapear(new EstoqueInsuficienteException(1, "Mouse", 3, 1));
        var segundo = ErrorCatalog.Mapear(new EstoqueInsuficienteException(2, "Monitor", 7, 0));

        segundo.Title.ShouldBe(primeiro.Title);
        segundo.Detail.ShouldNotBe(primeiro.Detail);
    }

    [Fact]
    public void Uses_a_generic_detail_for_every_unexpected_exception()
    {
        var primeiro = ErrorCatalog.Mapear(new InvalidOperationException("detalhe interno A"));
        var segundo = ErrorCatalog.Mapear(new ArgumentException("detalhe interno B"));

        segundo.Detail.ShouldBe(primeiro.Detail);
        segundo.Title.ShouldBe(primeiro.Title);
    }
}
