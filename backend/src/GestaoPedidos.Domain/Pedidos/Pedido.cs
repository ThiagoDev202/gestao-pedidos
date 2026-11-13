namespace GestaoPedidos.Domain.Pedidos;

public sealed class Pedido
{
    private readonly List<ItemPedido> _itens = [];

    public int Id { get; private set; }

    public int ClienteId { get; private set; }

    public DateTime DataPedido { get; private set; }

    public decimal ValorTotal { get; private set; }

    public IReadOnlyCollection<ItemPedido> Itens => _itens;

    private Pedido()
    {
    }

    public static Pedido Criar(int clienteId, IReadOnlyCollection<ItemParaPedido> itens, DateTime agoraUtc)
    {
        throw new NotImplementedException();
    }
}
