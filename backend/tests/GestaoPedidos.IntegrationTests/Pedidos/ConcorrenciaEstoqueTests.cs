using System.Net;
using GestaoPedidos.IntegrationTests.Support;
using Shouldly;

namespace GestaoPedidos.IntegrationTests.Pedidos;

public sealed class ConcorrenciaEstoqueTests(SqlServerFixture sql) : TesteDeIntegracao(sql)
{
    [Fact]
    public async Task Accepts_exactly_one_of_fifty_simultaneous_orders_for_the_last_unit()
    {
        var clienteId = await new ClienteBuilder().InserirAsync(Banco);
        var produtoId = await new ProdutoBuilder().ComEstoque(1).InserirAsync(Banco);
        var cliente = CriarCliente();
        using var largada = new SemaphoreSlim(0);

        var envios = Enumerable.Range(0, 50).Select(async _ =>
        {
            await largada.WaitAsync();
            return await cliente.CriarPedidoAsync(Respostas.Pedido(clienteId, (produtoId, 1)));
        }).ToList();
        largada.Release(50);
        var respostas = await Task.WhenAll(envios);

        respostas.Count(r => r.StatusCode == HttpStatusCode.Created).ShouldBe(1);
        var recusadas = respostas.Where(r => r.StatusCode != HttpStatusCode.Created).ToList();
        recusadas.Count.ShouldBe(49);
        foreach (var recusada in recusadas)
        {
            await recusada.DeveSerProblemaAsync(HttpStatusCode.UnprocessableEntity, "estoque_insuficiente");
        }

        (await Banco.EstoqueAsync(produtoId)).ShouldBe(0);
        (await Banco.ContarPedidosAsync()).ShouldBe(1);
        (await Banco.ContarItensAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task Completes_simultaneous_orders_with_products_in_opposite_orders_without_deadlock()
    {
        var clienteId = await new ClienteBuilder().InserirAsync(Banco);
        var produtoA = await new ProdutoBuilder().ComEstoque(500).InserirAsync(Banco);
        var produtoB = await new ProdutoBuilder().ComEstoque(500).InserirAsync(Banco);
        var produtoC = await new ProdutoBuilder().ComEstoque(500).InserirAsync(Banco);
        var cliente = CriarCliente();
        (int, int)[][] formatos =
        [
            [(produtoA, 1), (produtoB, 2), (produtoC, 3)],
            [(produtoC, 3), (produtoB, 2), (produtoA, 1)],
            [(produtoB, 2), (produtoA, 1)],
            [(produtoC, 1), (produtoA, 2)],
        ];
        var pedidos = Enumerable.Range(0, 40).Select(i => formatos[i % formatos.Length]).ToList();
        using var largada = new SemaphoreSlim(0);

        var envios = pedidos.Select(async itens =>
        {
            await largada.WaitAsync();
            return await cliente.CriarPedidoAsync(Respostas.Pedido(clienteId, itens));
        }).ToList();
        largada.Release(pedidos.Count);
        var respostas = await Task.WhenAll(envios);

        foreach (var resposta in respostas)
        {
            resposta.StatusCode.ShouldBe(HttpStatusCode.Created, await resposta.Content.ReadAsStringAsync());
        }

        int Consumo(int produtoId) => pedidos.SelectMany(p => p).Where(i => i.Item1 == produtoId).Sum(i => i.Item2);
        (await Banco.EstoqueAsync(produtoA)).ShouldBe(500 - Consumo(produtoA));
        (await Banco.EstoqueAsync(produtoB)).ShouldBe(500 - Consumo(produtoB));
        (await Banco.EstoqueAsync(produtoC)).ShouldBe(500 - Consumo(produtoC));
        (await Banco.ContarPedidosAsync()).ShouldBe(pedidos.Count);
    }
}
