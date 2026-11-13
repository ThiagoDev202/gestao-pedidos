using System.Text.Json;
using GestaoPedidos.IntegrationTests.Support;
using Shouldly;
using YamlDotNet.RepresentationModel;

namespace GestaoPedidos.IntegrationTests.Contrato;

public sealed class OpenApiContractTests(SqlServerFixture sql) : TesteDeIntegracao(sql)
{
    private static readonly string[] MetodosHttp = ["get", "put", "post", "delete", "patch", "head", "options", "trace"];

    private static readonly string[] CaminhosNaoDocumentados = ["/health"];

    private static readonly Lazy<IReadOnlyList<OperacaoDoContrato>> Contrato = new(LerContrato);

    public static TheoryData<string, string> Operacoes()
    {
        var dados = new TheoryData<string, string>();
        foreach (var operacao in Contrato.Value)
        {
            dados.Add(operacao.Caminho, operacao.Metodo);
        }

        return dados;
    }

    [Fact]
    public void Contract_file_lists_every_business_operation()
    {
        Contrato.Value.Select(o => $"{o.Metodo.ToUpperInvariant()} {o.Caminho}").ShouldBe(
            [
                "POST /pedidos",
                "GET /pedidos",
                "GET /pedidos/{id}",
                "GET /relatorios/top-clientes",
                "GET /clientes",
                "GET /produtos",
            ],
            ignoreOrder: true);
    }

    [Theory]
    [MemberData(nameof(Operacoes))]
    public async Task Generated_document_declares_the_contract_operation(string caminho, string metodo)
    {
        var documento = await DocumentoGeradoAsync();

        var caminhos = documento.GetProperty("paths");
        caminhos.TryGetProperty(caminho, out var item).ShouldBeTrue(
            $"O documento gerado não tem o caminho {caminho}. Caminhos gerados: {string.Join(", ", caminhos.EnumerateObject().Select(p => p.Name))}");
        item.TryGetProperty(metodo, out _).ShouldBeTrue($"O documento gerado não tem {metodo.ToUpperInvariant()} {caminho}.");
    }

    [Theory]
    [MemberData(nameof(Operacoes))]
    public async Task Generated_document_declares_every_contract_response_status(string caminho, string metodo)
    {
        var esperado = Operacao(caminho, metodo).Status;
        var operacao = OperacaoGerada(await DocumentoGeradoAsync(), caminho, metodo);

        var gerados = operacao.TryGetProperty("responses", out var respostas)
            ? respostas.EnumerateObject().Select(r => r.Name).ToList()
            : [];

        esperado.Except(gerados).ShouldBeEmpty(
            $"{metodo.ToUpperInvariant()} {caminho}: status do contrato ausentes no documento gerado (gerados: {string.Join(", ", gerados)}).");
    }

    [Theory]
    [MemberData(nameof(Operacoes))]
    public async Task Generated_document_declares_every_contract_query_and_header_parameter(string caminho, string metodo)
    {
        var esperado = Operacao(caminho, metodo).Parametros;
        var documento = await DocumentoGeradoAsync();
        var operacao = OperacaoGerada(documento, caminho, metodo);
        var gerados = ParametrosGerados(documento, documento.GetProperty("paths").GetProperty(caminho))
            .Concat(ParametrosGerados(documento, operacao))
            .ToList();

        var ausentes = esperado
            .Where(e => !gerados.Any(g => g.Local == e.Local && string.Equals(
                g.Nome,
                e.Nome,
                e.Local == "header" ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)))
            .Select(e => $"{e.Local}:{e.Nome}")
            .ToList();

        ausentes.ShouldBeEmpty(
            $"{metodo.ToUpperInvariant()} {caminho}: parâmetros do contrato ausentes no documento gerado (gerados: {string.Join(", ", gerados.Select(g => $"{g.Local}:{g.Nome}"))}).");
    }

    private async Task<JsonElement> DocumentoGeradoAsync()
    {
        var resposta = await CriarCliente().GetAsync("/openapi/v1.json");
        var corpo = await resposta.Content.ReadAsStringAsync();
        resposta.IsSuccessStatusCode.ShouldBeTrue(corpo);
        return JsonElement.Parse(corpo);
    }

    private static JsonElement OperacaoGerada(JsonElement documento, string caminho, string metodo)
    {
        documento.GetProperty("paths").TryGetProperty(caminho, out var item).ShouldBeTrue($"O documento gerado não tem o caminho {caminho}.");
        item.TryGetProperty(metodo, out var operacao).ShouldBeTrue($"O documento gerado não tem {metodo.ToUpperInvariant()} {caminho}.");
        return operacao;
    }

    private static IEnumerable<ParametroDoContrato> ParametrosGerados(JsonElement documento, JsonElement elemento)
    {
        if (!elemento.TryGetProperty("parameters", out var parametros))
        {
            yield break;
        }

        foreach (var parametro in parametros.EnumerateArray())
        {
            var resolvido = parametro.TryGetProperty("$ref", out var referencia)
                ? Resolver(documento, referencia.GetString()!)
                : parametro;
            yield return new ParametroDoContrato(resolvido.GetProperty("name").GetString()!, resolvido.GetProperty("in").GetString()!);
        }
    }

    private static JsonElement Resolver(JsonElement documento, string referencia)
    {
        var atual = documento;
        foreach (var parte in referencia.TrimStart('#', '/').Split('/'))
        {
            atual = atual.GetProperty(parte.Replace("~1", "/", StringComparison.Ordinal).Replace("~0", "~", StringComparison.Ordinal));
        }

        return atual;
    }

    private static OperacaoDoContrato Operacao(string caminho, string metodo)
    {
        return Contrato.Value.Single(o => o.Caminho == caminho && o.Metodo == metodo);
    }

    private static List<OperacaoDoContrato> LerContrato()
    {
        var yaml = new YamlStream();
        using (var leitor = new StreamReader(CaminhoDoContrato()))
        {
            yaml.Load(leitor);
        }

        var raiz = (YamlMappingNode)yaml.Documents[0].RootNode;
        var caminhos = (YamlMappingNode)raiz.Children[new YamlScalarNode("paths")];
        var operacoes = new List<OperacaoDoContrato>();
        foreach (var (chaveDoCaminho, noDoCaminho) in caminhos.Children)
        {
            var caminho = ((YamlScalarNode)chaveDoCaminho).Value!;
            if (CaminhosNaoDocumentados.Contains(caminho))
            {
                continue;
            }

            var item = (YamlMappingNode)noDoCaminho;
            var parametrosDoCaminho = Parametros(item);
            foreach (var (chaveDoMetodo, noDaOperacao) in item.Children)
            {
                var metodo = ((YamlScalarNode)chaveDoMetodo).Value!;
                if (!MetodosHttp.Contains(metodo))
                {
                    continue;
                }

                var operacao = (YamlMappingNode)noDaOperacao;
                var status = ((YamlMappingNode)operacao.Children[new YamlScalarNode("responses")]).Children.Keys
                    .Select(k => ((YamlScalarNode)k).Value!)
                    .ToList();
                operacoes.Add(new OperacaoDoContrato(caminho, metodo, status, parametrosDoCaminho.Concat(Parametros(operacao)).ToList()));
            }
        }

        return operacoes;
    }

    private static List<ParametroDoContrato> Parametros(YamlMappingNode no)
    {
        if (!no.Children.TryGetValue(new YamlScalarNode("parameters"), out var parametros))
        {
            return [];
        }

        return ((YamlSequenceNode)parametros).Children
            .Cast<YamlMappingNode>()
            .Select(p => new ParametroDoContrato(
                ((YamlScalarNode)p.Children[new YamlScalarNode("name")]).Value!,
                ((YamlScalarNode)p.Children[new YamlScalarNode("in")]).Value!))
            .Where(p => p.Local is "query" or "header")
            .ToList();
    }

    private static string CaminhoDoContrato()
    {
        var relativo = Path.Combine("specs", "001-gestao-pedidos", "contracts", "openapi.yaml");
        var diretorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (diretorio is not null)
        {
            var ehRaiz = Directory.Exists(Path.Combine(diretorio.FullName, ".git"))
                || File.Exists(Path.Combine(diretorio.FullName, ".git"))
                || File.Exists(Path.Combine(diretorio.FullName, "backend", "GestaoPedidos.slnx"));
            if (ehRaiz && File.Exists(Path.Combine(diretorio.FullName, relativo)))
            {
                return Path.Combine(diretorio.FullName, relativo);
            }

            diretorio = diretorio.Parent;
        }

        throw new FileNotFoundException("Contrato OpenAPI não encontrado acima de " + AppContext.BaseDirectory, relativo);
    }

    private sealed record ParametroDoContrato(string Nome, string Local);

    private sealed record OperacaoDoContrato(string Caminho, string Metodo, IReadOnlyList<string> Status, IReadOnlyList<ParametroDoContrato> Parametros);
}
