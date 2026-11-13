using System.Net;
using GestaoPedidos.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace GestaoPedidos.IntegrationTests.Pedidos;

public sealed class FalhaTransitoriaTests(SqlServerFixture sql) : TesteDeIntegracao(sql)
{
    private const int Deadlock = 1205;

    [Fact]
    public async Task Creates_the_order_once_and_decrements_stock_once_after_a_transient_failure()
    {
        var clienteId = await new ClienteBuilder().InserirAsync(Banco);
        var produtoA = await new ProdutoBuilder().ComPreco(20m).ComEstoque(10).InserirAsync(Banco);
        var produtoB = await new ProdutoBuilder().ComPreco(5m).ComEstoque(10).InserirAsync(Banco);
        var interceptor = new FalhaTransitoriaInterceptor(Deadlock);
        var cliente = CriarCliente(servicos: s => s.AddSingleton<IInterceptor>(interceptor));

        var resposta = await cliente.CriarPedidoAsync(Respostas.Pedido(clienteId, (produtoA, 2), (produtoB, 3)));

        resposta.StatusCode.ShouldBe(HttpStatusCode.Created, await resposta.Content.ReadAsStringAsync());
        interceptor.Falhas.ShouldBe(1, "a falha transitória precisa ser injetada na baixa de estoque feita pelo DbContext");
        (await Banco.ContarPedidosAsync()).ShouldBe(1);
        (await Banco.ContarItensAsync()).ShouldBe(2);
        (await Banco.EstoqueAsync(produtoA)).ShouldBe(8);
        (await Banco.EstoqueAsync(produtoB)).ShouldBe(7);
        var id = (await resposta.JsonAsync()).GetProperty("id").GetInt32();
        (await Banco.PedidoAsync(id)).ValorTotal.ShouldBe(55m);
    }

    [Fact]
    public async Task Records_the_idempotency_key_once_after_a_transient_failure()
    {
        var clienteId = await new ClienteBuilder().InserirAsync(Banco);
        var produtoId = await new ProdutoBuilder().ComEstoque(10).InserirAsync(Banco);
        var interceptor = new FalhaTransitoriaInterceptor(Deadlock);
        var cliente = CriarCliente(servicos: s => s.AddSingleton<IInterceptor>(interceptor));
        var chave = Guid.NewGuid().ToString();
        var corpo = Respostas.Pedido(clienteId, (produtoId, 4));

        var resposta = await cliente.CriarPedidoAsync(corpo, chave);
        var reenvio = await cliente.CriarPedidoAsync(corpo, chave);

        resposta.StatusCode.ShouldBe(HttpStatusCode.Created, await resposta.Content.ReadAsStringAsync());
        interceptor.Falhas.ShouldBe(1, "a falha transitória precisa ser injetada na baixa de estoque feita pelo DbContext");
        reenvio.StatusCode.ShouldBe(HttpStatusCode.OK, await reenvio.Content.ReadAsStringAsync());
        (await Banco.ContarPedidosAsync()).ShouldBe(1);
        (await Banco.ContarRegistrosDeIdempotenciaAsync()).ShouldBe(1);
        (await Banco.EstoqueAsync(produtoId)).ShouldBe(6);
    }
}
