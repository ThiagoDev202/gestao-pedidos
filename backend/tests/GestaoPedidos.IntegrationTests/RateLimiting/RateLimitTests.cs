using System.Net;
using GestaoPedidos.IntegrationTests.Support;
using Shouldly;

namespace GestaoPedidos.IntegrationTests.RateLimiting;

public sealed class RateLimitTests(SqlServerFixture sql) : TesteDeIntegracao(sql)
{
    private static readonly IPAddress IpDoCliente = IPAddress.Parse("203.0.113.10");

    private static Dictionary<string, string?> Limites(int tokensPost, int permissoesGlobais) => new()
    {
        ["RateLimiting:Enabled"] = "true",
        ["RateLimiting:PostPedidos:TokenLimit"] = tokensPost.ToString(System.Globalization.CultureInfo.InvariantCulture),
        ["RateLimiting:PostPedidos:TokensPerPeriod"] = "1",
        ["RateLimiting:PostPedidos:ReplenishmentPeriodSeconds"] = "60",
        ["RateLimiting:Global:PermitLimit"] = permissoesGlobais.ToString(System.Globalization.CultureInfo.InvariantCulture),
        ["RateLimiting:Global:WindowSeconds"] = "60",
    };

    private static async Task DeveSerLimiteExcedidoAsync(HttpResponseMessage resposta)
    {
        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.TooManyRequests, "limite_requisicoes_excedido");
        resposta.Headers.RetryAfter.ShouldNotBeNull();
        resposta.Headers.RetryAfter.Delta.ShouldNotBeNull();
        var segundos = (int)resposta.Headers.RetryAfter.Delta.Value.TotalSeconds;
        segundos.ShouldBeGreaterThan(0);
        problema.GetProperty("meta").GetProperty("retryAfterSegundos").GetInt32().ShouldBe(segundos);
    }

    [Fact]
    public async Task Rejects_the_post_beyond_the_token_limit_with_429_and_retry_after()
    {
        var clienteId = await new ClienteBuilder().InserirAsync(Banco);
        var produtoId = await new ProdutoBuilder().ComEstoque(100).InserirAsync(Banco);
        var cliente = CriarCliente(Limites(tokensPost: 3, permissoesGlobais: 1000), ipRemoto: IpDoCliente);

        for (var i = 0; i < 3; i++)
        {
            var aceita = await cliente.CriarPedidoAsync(Respostas.Pedido(clienteId, (produtoId, 1)));
            aceita.StatusCode.ShouldBe(HttpStatusCode.Created, await aceita.Content.ReadAsStringAsync());
        }

        var excedente = await cliente.CriarPedidoAsync(Respostas.Pedido(clienteId, (produtoId, 1)));

        await DeveSerLimiteExcedidoAsync(excedente);
        (await Banco.ContarPedidosAsync()).ShouldBe(3);
        (await Banco.EstoqueAsync(produtoId)).ShouldBe(97);
    }

    [Fact]
    public async Task Does_not_apply_the_order_creation_limit_to_reads()
    {
        var clienteId = await new ClienteBuilder().InserirAsync(Banco);
        var produtoId = await new ProdutoBuilder().ComEstoque(100).InserirAsync(Banco);
        var cliente = CriarCliente(Limites(tokensPost: 1, permissoesGlobais: 1000), ipRemoto: IpDoCliente);
        await cliente.CriarPedidoAsync(Respostas.Pedido(clienteId, (produtoId, 1)));
        var excedente = await cliente.CriarPedidoAsync(Respostas.Pedido(clienteId, (produtoId, 1)));
        await DeveSerLimiteExcedidoAsync(excedente);

        for (var i = 0; i < 5; i++)
        {
            var leitura = await cliente.GetAsync("/pedidos/1");
            leitura.StatusCode.ShouldNotBe(HttpStatusCode.TooManyRequests);
        }
    }

    [Fact]
    public async Task Rejects_any_request_beyond_the_global_limit_with_429_and_retry_after()
    {
        var cliente = CriarCliente(Limites(tokensPost: 1000, permissoesGlobais: 2), ipRemoto: IpDoCliente);

        for (var i = 0; i < 2; i++)
        {
            var aceita = await cliente.GetAsync("/pedidos/999999");
            aceita.StatusCode.ShouldNotBe(HttpStatusCode.TooManyRequests);
        }

        var excedente = await cliente.GetAsync("/pedidos/999999");

        await DeveSerLimiteExcedidoAsync(excedente);
    }

    [Fact]
    public async Task Does_not_limit_requests_when_rate_limiting_is_disabled()
    {
        var clienteId = await new ClienteBuilder().InserirAsync(Banco);
        var produtoId = await new ProdutoBuilder().ComEstoque(100).InserirAsync(Banco);
        var configuracao = Limites(tokensPost: 1, permissoesGlobais: 1);
        configuracao["RateLimiting:Enabled"] = "false";
        var cliente = CriarCliente(configuracao, ipRemoto: IpDoCliente);

        for (var i = 0; i < 4; i++)
        {
            var resposta = await cliente.CriarPedidoAsync(Respostas.Pedido(clienteId, (produtoId, 1)));
            resposta.StatusCode.ShouldBe(HttpStatusCode.Created, await resposta.Content.ReadAsStringAsync());
        }
    }
}
