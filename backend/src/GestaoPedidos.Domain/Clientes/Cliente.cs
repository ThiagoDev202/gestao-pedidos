namespace GestaoPedidos.Domain.Clientes;

public sealed class Cliente
{
    public int Id { get; init; }

    public required string Nome { get; init; }

    public required string Email { get; init; }
}
