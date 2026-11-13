using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;

namespace GestaoPedidos.IntegrationTests.Support;

public static class Respostas
{
    public const string ProblemJson = "application/problem+json";

    public static async Task<JsonElement> DeveSerProblemaAsync(this HttpResponseMessage resposta, HttpStatusCode status, string code)
    {
        var corpo = await resposta.Content.ReadAsStringAsync();
        resposta.StatusCode.ShouldBe(status, corpo);
        (resposta.Content.Headers.ContentType?.MediaType).ShouldBe(ProblemJson, corpo);

        var raiz = JsonElement.Parse(corpo);
        raiz.GetProperty("status").GetInt32().ShouldBe((int)status);
        raiz.GetProperty("code").GetString().ShouldBe(code);
        raiz.GetProperty("type").GetString().ShouldBe("urn:gestaopedidos:erro:" + code);
        raiz.GetProperty("title").GetString().ShouldNotBeNullOrWhiteSpace();
        raiz.GetProperty("detail").GetString().ShouldNotBeNullOrWhiteSpace();
        raiz.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        return raiz;
    }

    public static IReadOnlyList<(string Campo, string Code, string Mensagem)> ErrosDeCampo(this JsonElement problema)
    {
        return problema.GetProperty("errors").EnumerateArray()
            .Select(e => (
                e.GetProperty("campo").GetString()!,
                e.GetProperty("code").GetString()!,
                e.GetProperty("mensagem").GetString()!))
            .ToList();
    }

    public static async Task<JsonElement> JsonAsync(this HttpResponseMessage resposta)
    {
        var corpo = await resposta.Content.ReadAsStringAsync();
        return JsonElement.Parse(corpo);
    }

    public static Task<HttpResponseMessage> CriarPedidoAsync(
        this HttpClient cliente,
        object corpo,
        string? chaveIdempotencia = null,
        string? ipEncaminhado = null)
    {
        var requisicao = new HttpRequestMessage(HttpMethod.Post, "/pedidos")
        {
            Content = JsonContent.Create(corpo),
        };
        if (chaveIdempotencia is not null)
        {
            requisicao.Headers.TryAddWithoutValidation("Idempotency-Key", chaveIdempotencia);
        }

        if (ipEncaminhado is not null)
        {
            requisicao.Headers.TryAddWithoutValidation("X-Forwarded-For", ipEncaminhado);
        }

        return cliente.SendAsync(requisicao);
    }

    public static object Pedido(int clienteId, params (int ProdutoId, int Quantidade)[] itens)
    {
        return new
        {
            clienteId,
            itens = itens.Select(i => new { produtoId = i.ProdutoId, quantidade = i.Quantidade }).ToArray(),
        };
    }
}
