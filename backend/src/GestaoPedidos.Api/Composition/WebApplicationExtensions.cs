using GestaoPedidos.Api.Endpoints;
using GestaoPedidos.Api.Errors;
using Scalar.AspNetCore;

namespace GestaoPedidos.Api.Composition;

public static class WebApplicationExtensions
{
    public static WebApplication UseGestaoPedidos(this WebApplication app)
    {
        if (app.Configuration.GetValue<bool>("ForwardedHeaders:Enabled"))
        {
            app.UseForwardedHeaders();
        }

        app.UseExceptionHandler(_ => { });
        app.UseStatusCodePages(async contexto =>
        {
            var resposta = contexto.HttpContext.Response;
            var problema = resposta.StatusCode switch
            {
                StatusCodes.Status404NotFound => ErrorCatalog.NaoEncontrado(),
                StatusCodes.Status415UnsupportedMediaType => ErrorCatalog.FormatoNaoSuportado(),
                _ => null,
            };

            if (problema is not null)
            {
                await ProblemaWriter.EscreverAsync(contexto.HttpContext, problema);
            }
        });
        app.UseRateLimiter();

        app.MapHealthChecks("/health");
        app.MapOpenApi();
        app.MapScalarApiReference();
        app.MapPedidos();
        return app;
    }
}
