namespace GestaoPedidos.Application.Abstractions;

public interface IClock
{
    DateTime UtcNow { get; }
}
