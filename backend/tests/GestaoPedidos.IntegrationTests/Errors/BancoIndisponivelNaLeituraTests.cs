using System.Diagnostics;
using System.Net;
using GestaoPedidos.IntegrationTests.Support;
using Shouldly;

namespace GestaoPedidos.IntegrationTests.Errors;

public sealed class BancoIndisponivelNaLeituraTests(SqlServerFixture sql) : TesteDeIntegracao(sql)
{
    private static readonly TimeSpan TempoMaximo = TimeSpan.FromSeconds(30);

    private HttpClient ClienteSemBanco()
    {
        var cliente = CriarApi(connectionString: Sql.ConnectionStringInacessivel()).CreateClient();
        cliente.Timeout = TempoMaximo;
        return cliente;
    }

    [Fact]
    public async Task Returns_503_when_listing_orders_with_the_database_unavailable()
    {
        var cronometro = Stopwatch.StartNew();

        var resposta = await ClienteSemBanco().ListarPedidosAsync();

        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.ServiceUnavailable, "banco_indisponivel");
        problema.GetRawText().ShouldNotContain("127.0.0.1");
        cronometro.Elapsed.ShouldBeLessThan(TempoMaximo);
    }

    [Fact]
    public async Task Returns_503_when_reading_top_customers_with_the_database_unavailable()
    {
        var cronometro = Stopwatch.StartNew();

        var resposta = await ClienteSemBanco().TopClientesAsync("dataInicio=2026-03-01&dataFim=2026-03-31");

        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.ServiceUnavailable, "banco_indisponivel");
        problema.GetRawText().ShouldNotContain("127.0.0.1");
        cronometro.Elapsed.ShouldBeLessThan(TempoMaximo);
    }
}
