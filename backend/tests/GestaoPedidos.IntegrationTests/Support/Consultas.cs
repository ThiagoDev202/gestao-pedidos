using System.Globalization;
using System.Net;
using System.Text.Json;
using Shouldly;

namespace GestaoPedidos.IntegrationTests.Support;

public sealed record ResumoLido(int Id, int ClienteId, string ClienteNome, DateTime DataPedido, decimal ValorTotal, int QuantidadeItens);

public sealed record PaginaLida(IReadOnlyList<ResumoLido> Items, int Page, int PageSize, int TotalItems, int TotalPages)
{
    public IReadOnlyList<int> Ids => Items.Select(i => i.Id).ToList();
}

public sealed record TopClienteLido(int ClienteId, string Nome, decimal ValorTotal, int QuantidadePedidos);

public static class Consultas
{
    public static Task<HttpResponseMessage> ListarPedidosAsync(this HttpClient cliente, string consulta = "")
    {
        return cliente.GetAsync(string.IsNullOrEmpty(consulta) ? "/pedidos" : "/pedidos?" + consulta);
    }

    public static Task<HttpResponseMessage> TopClientesAsync(this HttpClient cliente, string consulta)
    {
        return cliente.GetAsync("/relatorios/top-clientes?" + consulta);
    }

    public static async Task<PaginaLida> DeveSerPaginaAsync(this HttpResponseMessage resposta)
    {
        var corpo = await resposta.DeveSerOkAsync();
        var itens = corpo.GetProperty("items").EnumerateArray()
            .Select(i => new ResumoLido(
                i.GetProperty("id").GetInt32(),
                i.GetProperty("clienteId").GetInt32(),
                i.GetProperty("clienteNome").GetString()!,
                i.DataUtc("dataPedido"),
                i.GetProperty("valorTotal").GetDecimal(),
                i.GetProperty("quantidadeItens").GetInt32()))
            .ToList();
        return new PaginaLida(
            itens,
            corpo.GetProperty("page").GetInt32(),
            corpo.GetProperty("pageSize").GetInt32(),
            corpo.GetProperty("totalItems").GetInt32(),
            corpo.GetProperty("totalPages").GetInt32());
    }

    public static async Task<IReadOnlyList<TopClienteLido>> DeveSerRankingAsync(this HttpResponseMessage resposta)
    {
        var corpo = await resposta.DeveSerOkAsync();
        corpo.ValueKind.ShouldBe(JsonValueKind.Array);
        return corpo.EnumerateArray()
            .Select(c => new TopClienteLido(
                c.GetProperty("clienteId").GetInt32(),
                c.GetProperty("nome").GetString()!,
                c.GetProperty("valorTotal").GetDecimal(),
                c.GetProperty("quantidadePedidos").GetInt32()))
            .ToList();
    }

    public static async Task<JsonElement> DeveSerOkAsync(this HttpResponseMessage resposta)
    {
        var corpo = await resposta.Content.ReadAsStringAsync();
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK, corpo);
        (resposta.Content.Headers.ContentType?.MediaType).ShouldBe("application/json", corpo);
        return JsonElement.Parse(corpo);
    }

    public static DateTime DataUtc(this JsonElement elemento, string propriedade)
    {
        var texto = elemento.GetProperty(propriedade).GetString()!;
        (texto.EndsWith('Z') || texto.EndsWith("+00:00", StringComparison.Ordinal)).ShouldBeTrue($"{propriedade} sem fuso UTC explícito: {texto}");
        var data = DateTimeOffset.Parse(texto, CultureInfo.InvariantCulture, DateTimeStyles.None);
        data.Offset.ShouldBe(TimeSpan.Zero);
        return data.UtcDateTime;
    }

    public static async Task<int> InserirEmAsync(this PedidoBuilder pedido, Banco banco, DateTime dataUtc)
    {
        var id = await pedido.InserirAsync(banco);
        await banco.ExecutarAsync(
            "UPDATE dbo.Pedidos SET DataPedido = CONVERT(datetime2(3), @data, 126) WHERE Id = @id",
            ("@data", dataUtc.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture)),
            ("@id", id));
        return id;
    }
}
