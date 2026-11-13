using System.Net;
using GestaoPedidos.IntegrationTests.Support;
using Shouldly;

namespace GestaoPedidos.IntegrationTests.Pedidos;

public sealed class IdempotenciaTests(SqlServerFixture sql) : TesteDeIntegracao(sql)
{
    private static async Task<HttpResponseMessage[]> EnviarSimultaneamenteAsync(HttpClient cliente, object corpo, string chave, int quantidade)
    {
        using var largada = new SemaphoreSlim(0);
        var envios = Enumerable.Range(0, quantidade).Select(async _ =>
        {
            await largada.WaitAsync();
            return await cliente.CriarPedidoAsync(corpo, chave);
        }).ToList();
        largada.Release(quantidade);
        return await Task.WhenAll(envios);
    }

    private static async Task<int> TodasApontamParaOMesmoPedidoAsync(HttpResponseMessage[] respostas)
    {
        foreach (var resposta in respostas)
        {
            ((int)resposta.StatusCode).ShouldBeOneOf([200, 201], await resposta.Content.ReadAsStringAsync());
        }

        respostas.Count(r => r.StatusCode == HttpStatusCode.Created).ShouldBe(1);
        respostas.Count(r => r.StatusCode == HttpStatusCode.OK).ShouldBe(respostas.Length - 1);
        var ids = new List<int>();
        foreach (var resposta in respostas)
        {
            var id = (await resposta.JsonAsync()).GetProperty("id").GetInt32();
            resposta.Headers.Location.ShouldNotBeNull();
            resposta.Headers.Location.OriginalString.ShouldBe($"/pedidos/{id}");
            ids.Add(id);
        }

        ids.Distinct().Count().ShouldBe(1);
        return ids[0];
    }

    [Fact]
    public async Task Creates_a_single_order_for_ten_simultaneous_requests_with_the_same_key()
    {
        var clienteId = await new ClienteBuilder().InserirAsync(Banco);
        var produtoId = await new ProdutoBuilder().ComEstoque(100).InserirAsync(Banco);
        var corpo = Respostas.Pedido(clienteId, (produtoId, 3));

        var respostas = await EnviarSimultaneamenteAsync(CriarCliente(), corpo, Guid.NewGuid().ToString(), 10);

        var pedidoId = await TodasApontamParaOMesmoPedidoAsync(respostas);
        (await Banco.ContarPedidosAsync()).ShouldBe(1);
        (await Banco.ContarRegistrosDeIdempotenciaAsync()).ShouldBe(1);
        (await Banco.EstoqueAsync(produtoId)).ShouldBe(97);
        (await Banco.PedidoAsync(pedidoId)).Itens.Single().Quantidade.ShouldBe(3);
    }

    [Fact]
    public async Task Returns_the_same_order_to_simultaneous_retries_when_stock_equals_the_requested_quantity()
    {
        var clienteId = await new ClienteBuilder().InserirAsync(Banco);
        var produtoId = await new ProdutoBuilder().ComEstoque(2).InserirAsync(Banco);
        var corpo = Respostas.Pedido(clienteId, (produtoId, 2));

        var respostas = await EnviarSimultaneamenteAsync(CriarCliente(), corpo, Guid.NewGuid().ToString(), 10);

        respostas.ShouldAllBe(r => r.StatusCode != HttpStatusCode.UnprocessableEntity);
        await TodasApontamParaOMesmoPedidoAsync(respostas);
        (await Banco.ContarPedidosAsync()).ShouldBe(1);
        (await Banco.EstoqueAsync(produtoId)).ShouldBe(0);
    }

    [Fact]
    public async Task Returns_200_with_the_original_order_on_a_sequential_retry()
    {
        var clienteId = await new ClienteBuilder().InserirAsync(Banco);
        var produtoId = await new ProdutoBuilder().ComEstoque(10).InserirAsync(Banco);
        var cliente = CriarCliente();
        var chave = Guid.NewGuid().ToString();
        var corpo = Respostas.Pedido(clienteId, (produtoId, 1));
        var primeira = await cliente.CriarPedidoAsync(corpo, chave);
        primeira.StatusCode.ShouldBe(HttpStatusCode.Created, await primeira.Content.ReadAsStringAsync());
        var original = await primeira.JsonAsync();

        var reenvio = await cliente.CriarPedidoAsync(corpo, chave);

        reenvio.StatusCode.ShouldBe(HttpStatusCode.OK, await reenvio.Content.ReadAsStringAsync());
        var repetido = await reenvio.JsonAsync();
        repetido.GetProperty("id").GetInt32().ShouldBe(original.GetProperty("id").GetInt32());
        repetido.GetProperty("valorTotal").GetDecimal().ShouldBe(original.GetProperty("valorTotal").GetDecimal());
        reenvio.Headers.Location.ShouldNotBeNull();
        reenvio.Headers.Location.ShouldBe(primeira.Headers.Location);
        (await Banco.ContarPedidosAsync()).ShouldBe(1);
        (await Banco.EstoqueAsync(produtoId)).ShouldBe(9);
    }

    [Fact]
    public async Task Treats_items_in_another_order_as_the_same_body()
    {
        var clienteId = await new ClienteBuilder().InserirAsync(Banco);
        var produtoA = await new ProdutoBuilder().InserirAsync(Banco);
        var produtoB = await new ProdutoBuilder().InserirAsync(Banco);
        var cliente = CriarCliente();
        var chave = Guid.NewGuid().ToString();
        var primeira = await cliente.CriarPedidoAsync(Respostas.Pedido(clienteId, (produtoA, 1), (produtoB, 2)), chave);
        primeira.StatusCode.ShouldBe(HttpStatusCode.Created, await primeira.Content.ReadAsStringAsync());

        var reenvio = await cliente.CriarPedidoAsync(Respostas.Pedido(clienteId, (produtoB, 2), (produtoA, 1)), chave);

        reenvio.StatusCode.ShouldBe(HttpStatusCode.OK, await reenvio.Content.ReadAsStringAsync());
        (await Banco.ContarPedidosAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task Rejects_the_same_key_with_a_different_body()
    {
        var clienteId = await new ClienteBuilder().InserirAsync(Banco);
        var produtoId = await new ProdutoBuilder().ComEstoque(10).InserirAsync(Banco);
        var cliente = CriarCliente();
        var chave = Guid.NewGuid().ToString();
        var primeira = await cliente.CriarPedidoAsync(Respostas.Pedido(clienteId, (produtoId, 1)), chave);
        primeira.StatusCode.ShouldBe(HttpStatusCode.Created, await primeira.Content.ReadAsStringAsync());

        var resposta = await cliente.CriarPedidoAsync(Respostas.Pedido(clienteId, (produtoId, 2)), chave);

        await resposta.DeveSerProblemaAsync(HttpStatusCode.UnprocessableEntity, "chave_reutilizada");
        (await Banco.ContarPedidosAsync()).ShouldBe(1);
        (await Banco.EstoqueAsync(produtoId)).ShouldBe(9);
    }

    [Fact]
    public async Task Rejects_the_same_key_for_another_customer()
    {
        var clienteA = await new ClienteBuilder().InserirAsync(Banco);
        var clienteB = await new ClienteBuilder().InserirAsync(Banco);
        var produtoId = await new ProdutoBuilder().ComEstoque(10).InserirAsync(Banco);
        var cliente = CriarCliente();
        var chave = Guid.NewGuid().ToString();
        var primeira = await cliente.CriarPedidoAsync(Respostas.Pedido(clienteA, (produtoId, 1)), chave);
        primeira.StatusCode.ShouldBe(HttpStatusCode.Created, await primeira.Content.ReadAsStringAsync());

        var resposta = await cliente.CriarPedidoAsync(Respostas.Pedido(clienteB, (produtoId, 1)), chave);

        await resposta.DeveSerProblemaAsync(HttpStatusCode.UnprocessableEntity, "chave_reutilizada");
        (await Banco.ContarPedidosAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task Creates_two_orders_for_the_same_content_with_different_keys()
    {
        var clienteId = await new ClienteBuilder().InserirAsync(Banco);
        var produtoId = await new ProdutoBuilder().ComEstoque(10).InserirAsync(Banco);
        var cliente = CriarCliente();
        var corpo = Respostas.Pedido(clienteId, (produtoId, 2));

        var primeira = await cliente.CriarPedidoAsync(corpo, Guid.NewGuid().ToString());
        var segunda = await cliente.CriarPedidoAsync(corpo, Guid.NewGuid().ToString());

        primeira.StatusCode.ShouldBe(HttpStatusCode.Created, await primeira.Content.ReadAsStringAsync());
        segunda.StatusCode.ShouldBe(HttpStatusCode.Created, await segunda.Content.ReadAsStringAsync());
        (await primeira.JsonAsync()).GetProperty("id").GetInt32().ShouldNotBe((await segunda.JsonAsync()).GetProperty("id").GetInt32());
        (await Banco.ContarPedidosAsync()).ShouldBe(2);
        (await Banco.EstoqueAsync(produtoId)).ShouldBe(6);
    }

    [Fact]
    public async Task Creates_two_orders_for_the_same_content_without_key()
    {
        var clienteId = await new ClienteBuilder().InserirAsync(Banco);
        var produtoId = await new ProdutoBuilder().ComEstoque(10).InserirAsync(Banco);
        var cliente = CriarCliente();
        var corpo = Respostas.Pedido(clienteId, (produtoId, 2));

        var primeira = await cliente.CriarPedidoAsync(corpo);
        var segunda = await cliente.CriarPedidoAsync(corpo);

        primeira.StatusCode.ShouldBe(HttpStatusCode.Created, await primeira.Content.ReadAsStringAsync());
        segunda.StatusCode.ShouldBe(HttpStatusCode.Created, await segunda.Content.ReadAsStringAsync());
        (await Banco.ContarPedidosAsync()).ShouldBe(2);
        (await Banco.ContarRegistrosDeIdempotenciaAsync()).ShouldBe(0);
        (await Banco.EstoqueAsync(produtoId)).ShouldBe(6);
    }

    [Fact]
    public async Task Does_not_keep_the_key_when_the_order_is_rejected()
    {
        var clienteId = await new ClienteBuilder().InserirAsync(Banco);
        var produtoId = await new ProdutoBuilder().ComEstoque(1).InserirAsync(Banco);
        var cliente = CriarCliente();
        var chave = Guid.NewGuid().ToString();
        var recusada = await cliente.CriarPedidoAsync(Respostas.Pedido(clienteId, (produtoId, 5)), chave);
        await recusada.DeveSerProblemaAsync(HttpStatusCode.UnprocessableEntity, "estoque_insuficiente");

        var corrigida = await cliente.CriarPedidoAsync(Respostas.Pedido(clienteId, (produtoId, 1)), chave);

        corrigida.StatusCode.ShouldBe(HttpStatusCode.Created, await corrigida.Content.ReadAsStringAsync());
        (await Banco.ContarRegistrosDeIdempotenciaAsync()).ShouldBe(1);
    }
}
