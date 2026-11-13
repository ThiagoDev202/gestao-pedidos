using System.Net;
using GestaoPedidos.Application.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GestaoPedidos.IntegrationTests.Support;

public sealed class ApiFactory(
    string connectionString,
    IReadOnlyDictionary<string, string?>? configuracoes = null,
    Action<IServiceCollection>? servicos = null,
    IPAddress? ipRemoto = null) : WebApplicationFactory<Program>
{
    public FakeClock Relogio { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:GestaoPedidos", connectionString);
        builder.UseSetting("RateLimiting:Enabled", "false");
        foreach (var (chave, valor) in configuracoes ?? new Dictionary<string, string?>())
        {
            builder.UseSetting(chave, valor);
        }

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IClock>();
            services.AddSingleton<IClock>(Relogio);
            if (ipRemoto is not null)
            {
                services.AddSingleton<IStartupFilter>(new IpRemotoFixoStartupFilter(ipRemoto));
            }

            servicos?.Invoke(services);
        });
    }
}
