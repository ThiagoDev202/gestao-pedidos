using GestaoPedidos.Application.Abstractions;

namespace GestaoPedidos.IntegrationTests.Support;

public sealed class FakeClock : IClock
{
    public static readonly DateTime Padrao = new(2026, 3, 15, 13, 45, 30, 123, DateTimeKind.Utc);

    public DateTime UtcNow { get; set; } = Padrao;
}
