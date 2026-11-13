using GestaoPedidos.Application.Pedidos.Dtos;
using GestaoPedidos.Application.Pedidos.Ports;
using GestaoPedidos.Application.Pedidos.Queries.ListarPedidos;
using NSubstitute;
using Shouldly;

namespace GestaoPedidos.UnitTests.Pedidos;

public sealed class ListarPedidosHandlerTests
{
    private readonly IPedidoQueries _consultas = Substitute.For<IPedidoQueries>();

    [Fact]
    public async Task Returns_the_page_read_by_the_queries_port_for_the_same_query()
    {
        var query = new ListarPedidosQuery(7, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31), 2, 10);
        var pagina = new PedidosPaginadosDto(
            [new PedidoResumoDto(42, 7, "Maria", new DateTime(2026, 3, 5, 10, 0, 0, DateTimeKind.Utc), 38.00m, 2)],
            2,
            10,
            11,
            2);
        using var cancelamento = new CancellationTokenSource();
        _consultas.ListarAsync(query, cancelamento.Token).Returns(pagina);

        var resultado = await new ListarPedidosHandler(_consultas).HandleAsync(query, cancelamento.Token);

        resultado.ShouldBeSameAs(pagina);
        await _consultas.Received(1).ListarAsync(query, cancelamento.Token);
    }

    [Fact]
    public async Task Returns_an_empty_page_from_the_queries_port()
    {
        var query = new ListarPedidosQuery(null, null, null, 1, 20);
        var vazia = new PedidosPaginadosDto([], 1, 20, 0, 0);
        _consultas.ListarAsync(query, Arg.Any<CancellationToken>()).Returns(vazia);

        var resultado = await new ListarPedidosHandler(_consultas).HandleAsync(query, CancellationToken.None);

        resultado.ShouldBeSameAs(vazia);
    }
}
