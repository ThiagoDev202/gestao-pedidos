using Microsoft.AspNetCore.Mvc;

namespace GestaoPedidos.Api.Contracts;

public sealed record ParametrosDeListagem(
    [FromQuery(Name = "clienteId")] string? ClienteId,
    [FromQuery(Name = "dataInicio")] string? DataInicio,
    [FromQuery(Name = "dataFim")] string? DataFim,
    [FromQuery(Name = "page")] string? Page,
    [FromQuery(Name = "pageSize")] string? PageSize);
