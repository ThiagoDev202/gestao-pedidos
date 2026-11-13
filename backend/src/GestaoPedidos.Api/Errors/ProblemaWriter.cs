using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace GestaoPedidos.Api.Errors;

public static class ProblemaWriter
{
    private static readonly JsonSerializerOptions Opcoes = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public static async Task EscreverAsync(HttpContext contexto, ProblemaHttp problema)
    {
        var corpo = new
        {
            type = problema.Type,
            title = problema.Title,
            status = problema.Status,
            detail = problema.Detail,
            code = problema.Code,
            traceId = Activity.Current?.Id ?? contexto.TraceIdentifier,
            meta = problema.Meta,
            errors = problema.Errors,
        };

        contexto.Response.StatusCode = problema.Status;
        contexto.Response.ContentType = "application/problem+json";
        await contexto.Response.WriteAsync(JsonSerializer.Serialize(corpo, Opcoes));
    }
}
