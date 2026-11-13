using System.Threading.RateLimiting;
using GestaoPedidos.Api.Errors;
using Microsoft.AspNetCore.RateLimiting;

namespace GestaoPedidos.Api.RateLimiting;

public static class RateLimitingExtensions
{
    public const string PoliticaCriarPedido = "criar-pedido";

    public static IServiceCollection AddRateLimitingPolicies(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection("RateLimiting").Get<RateLimitingSettings>() ?? new RateLimitingSettings();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = EscreverRejeicaoAsync;

            options.AddPolicy(PoliticaCriarPedido, contexto => settings.Enabled
                ? RateLimitPartition.GetTokenBucketLimiter(Origem(contexto), _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = settings.PostPedidos.TokenLimit,
                    TokensPerPeriod = settings.PostPedidos.TokensPerPeriod,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(settings.PostPedidos.ReplenishmentPeriodSeconds),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                })
                : RateLimitPartition.GetNoLimiter("sem-limite"));

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(contexto => settings.Enabled
                ? RateLimitPartition.GetFixedWindowLimiter(Origem(contexto), _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = settings.Global.PermitLimit,
                    Window = TimeSpan.FromSeconds(settings.Global.WindowSeconds),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                })
                : RateLimitPartition.GetNoLimiter("sem-limite"));
        });

        return services;
    }

    private static string Origem(HttpContext contexto) =>
        contexto.Connection.RemoteIpAddress?.ToString() ?? "origem-desconhecida";

    private static async ValueTask EscreverRejeicaoAsync(OnRejectedContext contexto, CancellationToken cancellationToken)
    {
        var segundos = contexto.Lease.TryGetMetadata(MetadataName.RetryAfter, out var espera)
            ? Math.Max(1, (int)Math.Ceiling(espera.TotalSeconds))
            : 1;

        contexto.HttpContext.Response.Headers.RetryAfter = segundos.ToString(System.Globalization.CultureInfo.InvariantCulture);
        await ProblemaWriter.EscreverAsync(contexto.HttpContext, ErrorCatalog.LimiteExcedido(segundos));
    }
}
