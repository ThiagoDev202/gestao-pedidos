using Microsoft.AspNetCore.Diagnostics;

namespace GestaoPedidos.Api.Errors;

public sealed partial class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problema = ErrorCatalog.Mapear(exception);
        if (problema.Status >= 500)
        {
            RegistrarFalha(logger, exception, problema.Code);
        }

        await ProblemaWriter.EscreverAsync(httpContext, problema);
        return true;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Falha ao processar a requisição ({Code})")]
    private static partial void RegistrarFalha(ILogger logger, Exception exception, string code);
}
