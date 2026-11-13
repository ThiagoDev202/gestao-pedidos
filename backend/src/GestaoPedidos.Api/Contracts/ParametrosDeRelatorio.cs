using Microsoft.AspNetCore.Mvc;

namespace GestaoPedidos.Api.Contracts;

public sealed record ParametrosDeRelatorio(
    [FromQuery(Name = "dataInicio")] string? DataInicio,
    [FromQuery(Name = "dataFim")] string? DataFim);
