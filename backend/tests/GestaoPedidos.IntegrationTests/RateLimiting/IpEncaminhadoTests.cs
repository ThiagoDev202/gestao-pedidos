using System.Net;
using GestaoPedidos.IntegrationTests.Support;
using Shouldly;

namespace GestaoPedidos.IntegrationTests.RateLimiting;

public sealed class IpEncaminhadoTests(SqlServerFixture sql) : TesteDeIntegracao(sql)
{
    private static readonly Dictionary<string, string?> Configuracao = new()
    {
        ["RateLimiting:Enabled"] = "true",
        ["RateLimiting:PostPedidos:TokenLimit"] = "1",
        ["RateLimiting:PostPedidos:TokensPerPeriod"] = "1",
        ["RateLimiting:PostPedidos:ReplenishmentPeriodSeconds"] = "60",
        ["RateLimiting:Global:PermitLimit"] = "1000",
        ["RateLimiting:Global:WindowSeconds"] = "60",
        ["ForwardedHeaders:Enabled"] = "true",
        ["ForwardedHeaders:KnownProxies:0"] = "127.0.0.1",
    };

    private async Task<object> PedidoValidoAsync()
    {
        var clienteId = await new ClienteBuilder().InserirAsync(Banco);
        var produtoId = await new ProdutoBuilder().ComEstoque(100).InserirAsync(Banco);
        return Respostas.Pedido(clienteId, (produtoId, 1));
    }

    [Fact]
    public async Task Limits_each_forwarded_client_separately_when_the_proxy_is_trusted()
    {
        var pedido = await PedidoValidoAsync();
        var cliente = CriarCliente(Configuracao, ipRemoto: IPAddress.Loopback);

        var primeiroCliente = await cliente.CriarPedidoAsync(pedido, ipEncaminhado: "198.51.100.1");
        var segundoCliente = await cliente.CriarPedidoAsync(pedido, ipEncaminhado: "198.51.100.2");
        var primeiroClienteDeNovo = await cliente.CriarPedidoAsync(pedido, ipEncaminhado: "198.51.100.1");

        primeiroCliente.StatusCode.ShouldBe(HttpStatusCode.Created, await primeiroCliente.Content.ReadAsStringAsync());
        segundoCliente.StatusCode.ShouldBe(HttpStatusCode.Created, await segundoCliente.Content.ReadAsStringAsync());
        await primeiroClienteDeNovo.DeveSerProblemaAsync(HttpStatusCode.TooManyRequests, "limite_requisicoes_excedido");
    }

    [Fact]
    public async Task Ignores_the_forwarded_header_from_an_untrusted_origin()
    {
        var pedido = await PedidoValidoAsync();
        var cliente = CriarCliente(Configuracao, ipRemoto: IPAddress.Parse("10.0.0.9"));

        var primeira = await cliente.CriarPedidoAsync(pedido, ipEncaminhado: "198.51.100.1");
        var segunda = await cliente.CriarPedidoAsync(pedido, ipEncaminhado: "198.51.100.2");

        primeira.StatusCode.ShouldBe(HttpStatusCode.Created, await primeira.Content.ReadAsStringAsync());
        await segunda.DeveSerProblemaAsync(HttpStatusCode.TooManyRequests, "limite_requisicoes_excedido");
    }

    [Fact]
    public async Task Ignores_the_forwarded_header_when_forwarded_headers_are_disabled()
    {
        var pedido = await PedidoValidoAsync();
        var configuracao = new Dictionary<string, string?>(Configuracao) { ["ForwardedHeaders:Enabled"] = "false" };
        var cliente = CriarCliente(configuracao, ipRemoto: IPAddress.Loopback);

        var primeira = await cliente.CriarPedidoAsync(pedido, ipEncaminhado: "198.51.100.1");
        var segunda = await cliente.CriarPedidoAsync(pedido, ipEncaminhado: "198.51.100.2");

        primeira.StatusCode.ShouldBe(HttpStatusCode.Created, await primeira.Content.ReadAsStringAsync());
        await segunda.DeveSerProblemaAsync(HttpStatusCode.TooManyRequests, "limite_requisicoes_excedido");
    }
}
