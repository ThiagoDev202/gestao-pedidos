using GestaoPedidos.Domain.Excecoes;

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
        if (itens.Count == 0)
        {
            throw new PedidoInvalidoException("O pedido precisa de ao menos um item.");
        }

        var invalido = itens.FirstOrDefault(item => item.Quantidade <= 0);
        if (invalido is not null)
        {
            throw new PedidoInvalidoException($"A quantidade do produto {invalido.ProdutoId} deve ser maior que zero.");
        }

        var pedido = new Pedido
        {
            ClienteId = clienteId,
            DataPedido = DateTime.SpecifyKind(agoraUtc, DateTimeKind.Utc),
        };

        foreach (var item in itens)
        {
            pedido._itens.Add(new ItemPedido(item.ProdutoId, item.Quantidade, item.PrecoUnitario));
        }

        pedido.ValorTotal = pedido._itens.Sum(item => item.Subtotal);
        return pedido;
    }
}
