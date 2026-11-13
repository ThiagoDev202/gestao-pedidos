using GestaoPedidos.Api.Errors;

namespace GestaoPedidos.Api.Endpoints;

public static class DocumentacaoExtensions
{
    public static RouteHandlerBuilder ComErros(this RouteHandlerBuilder builder, params int[] statusCodes)
    {
        foreach (var status in statusCodes)
        {
            builder.Produces<ProblemaHttp>(status, "application/problem+json");
        }

        return builder;
    }
}
