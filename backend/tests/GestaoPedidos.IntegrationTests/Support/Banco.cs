using Microsoft.Data.SqlClient;

namespace GestaoPedidos.IntegrationTests.Support;

public sealed class Banco(string connectionString)
{
    public async Task<int> ExecutarAsync(string sql, params (string Nome, object Valor)[] parametros)
    {
        await using var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync();
        await using var comando = Criar(conexao, sql, parametros);
        return await comando.ExecuteNonQueryAsync();
    }

    public async Task<T> EscalarAsync<T>(string sql, params (string Nome, object Valor)[] parametros)
    {
        await using var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync();
        await using var comando = Criar(conexao, sql, parametros);
        var resultado = await comando.ExecuteScalarAsync();
        return (T)Convert.ChangeType(resultado!, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
    }

    public Task<int> EstoqueAsync(int produtoId)
    {
        return EscalarAsync<int>("SELECT Estoque FROM dbo.Produtos WHERE Id = @id", ("@id", produtoId));
    }

    public Task<decimal> PrecoAsync(int produtoId)
    {
        return EscalarAsync<decimal>("SELECT Preco FROM dbo.Produtos WHERE Id = @id", ("@id", produtoId));
    }

    public Task<int> ContarPedidosAsync()
    {
        return EscalarAsync<int>("SELECT COUNT(*) FROM dbo.Pedidos");
    }

    public Task<int> ContarItensAsync()
    {
        return EscalarAsync<int>("SELECT COUNT(*) FROM dbo.ItensPedido");
    }

    public Task<int> ContarRegistrosDeIdempotenciaAsync()
    {
        return EscalarAsync<int>("SELECT COUNT(*) FROM dbo.IdempotenciaPedidos");
    }

    public async Task<PedidoGravado> PedidoAsync(int pedidoId)
    {
        await using var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync();
        await using var comando = Criar(
            conexao,
            "SELECT ClienteId, DataPedido, ValorTotal FROM dbo.Pedidos WHERE Id = @id",
            [("@id", pedidoId)]);
        await using var leitor = await comando.ExecuteReaderAsync();
        if (!await leitor.ReadAsync())
        {
            throw new InvalidOperationException($"Pedido {pedidoId} não está no banco.");
        }

        var pedido = new PedidoGravado(leitor.GetInt32(0), leitor.GetDateTime(1), leitor.GetDecimal(2), []);
        await leitor.CloseAsync();

        await using var itens = Criar(
            conexao,
            "SELECT ProdutoId, Quantidade, PrecoUnitario FROM dbo.ItensPedido WHERE PedidoId = @id ORDER BY ProdutoId",
            [("@id", pedidoId)]);
        await using var leitorItens = await itens.ExecuteReaderAsync();
        var lista = new List<ItemGravado>();
        while (await leitorItens.ReadAsync())
        {
            lista.Add(new ItemGravado(leitorItens.GetInt32(0), leitorItens.GetInt32(1), leitorItens.GetDecimal(2)));
        }

        return pedido with { Itens = lista };
    }

    private static SqlCommand Criar(SqlConnection conexao, string sql, (string Nome, object Valor)[] parametros)
    {
        var comando = new SqlCommand(sql, conexao);
        foreach (var (nome, valor) in parametros)
        {
            comando.Parameters.AddWithValue(nome, valor);
        }

        return comando;
    }
}

public sealed record PedidoGravado(int ClienteId, DateTime DataPedido, decimal ValorTotal, IReadOnlyList<ItemGravado> Itens);

public sealed record ItemGravado(int ProdutoId, int Quantidade, decimal PrecoUnitario);
