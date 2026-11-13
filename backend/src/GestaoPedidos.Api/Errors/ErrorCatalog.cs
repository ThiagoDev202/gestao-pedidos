using GestaoPedidos.Application.Errors;
using GestaoPedidos.Domain.Excecoes;
using Microsoft.AspNetCore.Http;

namespace GestaoPedidos.Api.Errors;

public static class ErrorCatalog
{
    private const string Prefixo = "urn:gestaopedidos:erro:";

    public static IReadOnlyCollection<string> TodosOsCodes { get; } =
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

    public static ProblemaHttp Mapear(Exception exception) => exception switch
    {
        ClienteInexistenteException e => Regra("cliente_inexistente", "Cliente inexistente", e.Message, ("clienteId", e.ClienteId)),
        ProdutoInexistenteException e => Regra("produto_inexistente", "Produto inexistente", e.Message, ("produtoId", e.ProdutoId)),
        EstoqueInsuficienteException e => Regra(
            "estoque_insuficiente",
            "Estoque insuficiente",
            e.Message,
            ("produtoId", e.ProdutoId),
            ("produtoNome", e.ProdutoNome),
            ("solicitado", e.Solicitado),
            ("disponivel", e.Disponivel)),
        ChaveReutilizadaException e => Regra("chave_reutilizada", "Chave de idempotência reutilizada", e.Message, ("chave", e.Chave)),
        PedidoNaoEncontradoException e => Criar(404, "pedido_nao_encontrado", "Pedido não encontrado", e.Message, new Dictionary<string, object?> { ["pedidoId"] = e.PedidoId }, null),
        PedidoInvalidoException e => Validacao([new ErroDeCampo("pedido", "pedido_invalido", e.Message)]),
        RequisicaoInvalidaException e => Validacao(e.Erros),
        BadHttpRequestException { StatusCode: StatusCodes.Status415UnsupportedMediaType } => FormatoNaoSuportado(),
        BadHttpRequestException => Validacao([new ErroDeCampo("$", "corpo_invalido", "O corpo da requisição não é um JSON válido ou contém campos não permitidos.")]),
        BancoIndisponivelException => BancoIndisponivel(),
        _ => Criar(500, "erro_interno", "Erro interno", "Ocorreu um erro inesperado. Informe o código de suporte ao contatar a equipe.", null, null),
    };

    public static ProblemaHttp NaoEncontrado() =>
        Criar(404, "recurso_nao_encontrado", "Recurso não encontrado", "O recurso solicitado não existe.", null, null);

    public static ProblemaHttp FormatoNaoSuportado() =>
        Criar(415, "formato_nao_suportado", "Formato não suportado", "O corpo da requisição deve ser enviado como application/json.", null, null);

    public static ProblemaHttp BancoIndisponivel() =>
        Criar(503, "banco_indisponivel", "Banco de dados indisponível", "O serviço está temporariamente indisponível. Tente novamente em instantes.", null, null);

    public static ProblemaHttp LimiteExcedido(int retryAfterSegundos) =>
        Criar(
            429,
            "limite_requisicoes_excedido",
            "Limite de requisições excedido",
            $"Muitas requisições. Tente novamente em {retryAfterSegundos} segundo(s).",
            new Dictionary<string, object?> { ["retryAfterSegundos"] = retryAfterSegundos },
            null);

    private static ProblemaHttp Regra(string code, string title, string detail, params (string Chave, object? Valor)[] meta) =>
        Criar(422, code, title, detail, meta.ToDictionary(m => m.Chave, m => m.Valor), null);

    private static ProblemaHttp Validacao(IReadOnlyList<ErroDeCampo> erros) =>
        Criar(400, "validacao_falhou", "Dados inválidos", "Um ou mais campos são inválidos. Corrija e tente novamente.", null, erros);

    private static ProblemaHttp Criar(
        int status,
        string code,
        string title,
        string detail,
        IReadOnlyDictionary<string, object?>? meta,
        IReadOnlyList<ErroDeCampo>? erros) =>
        new(status, Prefixo + code, title, detail, code, meta, erros);
}
