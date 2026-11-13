using GestaoPedidos.Application.Abstractions;

namespace GestaoPedidos.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
