using System.Globalization;

namespace GestaoPedidos.IntegrationTests.Support;

public sealed class ClienteBuilder
{
    private static int _sequencia;

    private string _nome = "Cliente de Teste";
    private string? _email;

    public ClienteBuilder ComNome(string nome)
    {
        _nome = nome;
        return this;
    }

    public ClienteBuilder ComEmail(string email)
    {
        _email = email;
        return this;
    }

    public Task<int> InserirAsync(Banco banco)
    {
        var email = _email ?? $"cliente{Interlocked.Increment(ref _sequencia)}@teste.local";
        return banco.EscalarAsync<int>(
            "INSERT INTO dbo.Clientes (Nome, Email) OUTPUT INSERTED.Id VALUES (@nome, @email)",
            ("@nome", _nome),
            ("@email", email));
    }
}

public sealed class ProdutoBuilder
{
    private static int _sequencia;

    private string? _nome;
    private string? _sku;
    private decimal _preco = 10m;
    private int _estoque = 100;

    public ProdutoBuilder ComNome(string nome)
    {
        _nome = nome;
        return this;
    }

    public ProdutoBuilder ComSku(string sku)
    {
        _sku = sku;
        return this;
    }

    public ProdutoBuilder ComPreco(decimal preco)
    {
        _preco = preco;
        return this;
    }

    public ProdutoBuilder ComEstoque(int estoque)
    {
        _estoque = estoque;
        return this;
    }

    public Task<int> InserirAsync(Banco banco)
    {
        var numero = Interlocked.Increment(ref _sequencia).ToString(CultureInfo.InvariantCulture);
        return banco.EscalarAsync<int>(
            "INSERT INTO dbo.Produtos (Nome, Sku, Preco, Estoque) OUTPUT INSERTED.Id VALUES (@nome, @sku, @preco, @estoque)",
            ("@nome", _nome ?? "Produto " + numero),
            ("@sku", _sku ?? "SKU-T-" + numero),
            ("@preco", _preco),
            ("@estoque", _estoque));
    }
}

public sealed class PedidoBuilder(int clienteId)
{
    private readonly List<(int ProdutoId, int Quantidade, decimal PrecoUnitario)> _itens = [];
    private DateTime _data = FakeClock.Padrao;

    public PedidoBuilder EmData(DateTime dataUtc)
    {
        _data = dataUtc;
        return this;
    }

    public PedidoBuilder ComItem(int produtoId, int quantidade, decimal precoUnitario)
    {
        _itens.Add((produtoId, quantidade, precoUnitario));
        return this;
    }

    public async Task<int> InserirAsync(Banco banco)
    {
        var total = _itens.Sum(i => i.Quantidade * i.PrecoUnitario);
        var pedidoId = await banco.EscalarAsync<int>(
            "INSERT INTO dbo.Pedidos (ClienteId, DataPedido, ValorTotal) OUTPUT INSERTED.Id VALUES (@cliente, @data, @total)",
            ("@cliente", clienteId),
            ("@data", _data),
            ("@total", total));
        foreach (var (produtoId, quantidade, preco) in _itens)
        {
            await banco.ExecutarAsync(
                "INSERT INTO dbo.ItensPedido (PedidoId, ProdutoId, Quantidade, PrecoUnitario) VALUES (@pedido, @produto, @quantidade, @preco)",
                ("@pedido", pedidoId),
                ("@produto", produtoId),
                ("@quantidade", quantidade),
                ("@preco", preco));
        }

        return pedidoId;
    }
}
