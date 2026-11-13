using System.Net;
using System.Text.Json.Serialization;
using GestaoPedidos.Api.Errors;
using GestaoPedidos.Api.RateLimiting;
using GestaoPedidos.Application;
using GestaoPedidos.Infrastructure;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Routing;

namespace GestaoPedidos.Api.Composition;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGestaoPedidos(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplication();
        services.AddInfrastructure(configuration);
        services.AddLoggingDecorators();

        services.Configure<JsonOptions>(opcoes =>
        {
            opcoes.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
            opcoes.SerializerOptions.RespectNullableAnnotations = true;
        });
        services.Configure<RouteHandlerOptions>(opcoes => opcoes.ThrowOnBadRequest = true);

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddRateLimitingPolicies(configuration);
        services.AddOpenApi();

        if (configuration.GetValue<bool>("ForwardedHeaders:Enabled"))
        {
            services.Configure<ForwardedHeadersOptions>(opcoes =>
            {
                opcoes.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

                foreach (var ip in configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
                {
                    opcoes.KnownProxies.Add(IPAddress.Parse(ip));
                }
            });
        }

        return services;
    }
}
