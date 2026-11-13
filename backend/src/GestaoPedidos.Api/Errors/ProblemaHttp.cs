using GestaoPedidos.Application.Errors;

namespace GestaoPedidos.Api.Errors;

public sealed record ProblemaHttp(
    int Status,
    string Type,
    string Title,
    string Detail,
    string Code,
    IReadOnlyDictionary<string, object?>? Meta,
    IReadOnlyList<ErroDeCampo>? Errors);
