using System.Net;
using GestaoPedidos.IntegrationTests.Support;
using Shouldly;

namespace GestaoPedidos.IntegrationTests.Pedidos;

public sealed class ConcorrenciaEstoqueMetaTests(SqlServerFixture sql) : TesteDeIntegracao(sql)
{
    [Fact]
    public async Task Losing_orders_for_the_last_unit_report_the_real_remaining_stock_in_meta()
    {
        var clienteId = await new ClienteBuilder().InserirAsync(Banco);
        var produtoId = await new ProdutoBuilder().ComEstoque(1).InserirAsync(Banco);
        var cliente = CriarCliente();
        using var largada = new SemaphoreSlim(0);

        var envios = Enumerable.Range(0, 20).Select(async _ =>
        {
            await largada.WaitAsync();
            return await cliente.CriarPedidoAsync(Respostas.Pedido(clienteId, (produtoId, 1)));
        }).ToList();
        largada.Release(20);
        var respostas = await Task.WhenAll(envios);

        respostas.Count(r => r.StatusCode == HttpStatusCode.Created).ShouldBe(1);
        var recusadas = respostas.Where(r => r.StatusCode != HttpStatusCode.Created).ToList();
        recusadas.Count.ShouldBe(19);
        foreach (var recusada in recusadas)
        {
            var problema = await recusada.DeveSerProblemaAsync(HttpStatusCode.UnprocessableEntity, "estoque_insuficiente");
            var meta = problema.GetProperty("meta");
            meta.GetProperty("produtoId").GetInt32().ShouldBe(produtoId);
            meta.GetProperty("solicitado").GetInt32().ShouldBe(1);
            meta.GetProperty("disponivel").GetInt32().ShouldBe(0, problema.GetRawText());
            meta.GetProperty("produtoNome").GetString().ShouldNotBeNullOrWhiteSpace();
        }

        (await Banco.EstoqueAsync(produtoId)).ShouldBe(0);
    }
}
