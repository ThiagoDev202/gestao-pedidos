using GestaoPedidos.Application.Abstractions;
using GestaoPedidos.Application.Apoio.Ports;
using GestaoPedidos.Application.Pedidos.Ports;
using GestaoPedidos.Application.Relatorios.Ports;
using GestaoPedidos.Infrastructure.Logging;
using GestaoPedidos.Infrastructure.Persistence;
using GestaoPedidos.Infrastructure.Queries;
using GestaoPedidos.Infrastructure.Resilience;
using GestaoPedidos.Infrastructure.Time;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Registry;
using Polly.Retry;

namespace GestaoPedidos.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("GestaoPedidos")
            ?? throw new InvalidOperationException("A connection string 'GestaoPedidos' não foi configurada.");

        services.AddDbContext<GestaoPedidosDbContext>((provider, options) =>
        {
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
            options.AddInterceptors(provider.GetServices<IInterceptor>());
        });

        services.AddResiliencePipeline(ConsultaSql.NomeDoPipeline, pipeline =>
            pipeline.AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<SqlException>(e => e.IsTransient),
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromMilliseconds(200),
                UseJitter = true,
            }));

        services.AddSingleton(provider => new ConsultaSql(connectionString, provider.GetRequiredService<ResiliencePipelineProvider<string>>()));
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IProdutoRepository, ProdutoRepository>();
        services.AddScoped<IPedidoRepository, PedidoRepository>();
        services.AddScoped<IIdempotenciaRepository, IdempotenciaRepository>();
        services.AddScoped<IPedidoQueries, PedidoQueries>();
        services.AddScoped<IRelatorioQueries, RelatorioQueries>();
        services.AddScoped<IApoioQueries, ApoioQueries>();

        services.AddHealthChecks().AddDbContextCheck<GestaoPedidosDbContext>("banco");
        return services;
    }

    public static IServiceCollection AddLoggingDecorators(this IServiceCollection services)
    {
        services.Decorate(typeof(ICommandHandler<,>), typeof(LoggingCommandDecorator<,>));
        services.Decorate(typeof(IQueryHandler<,>), typeof(LoggingQueryDecorator<,>));
        return services;
    }
}
