namespace GestaoPedidos.Application.Relatorios.Queries.TopClientes;

public sealed record TopClientesQuery(DateOnly? DataInicio, DateOnly? DataFim);

public sealed record TopClienteDto(int ClienteId, string Nome, decimal ValorTotal, int QuantidadePedidos);
