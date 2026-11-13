using GestaoPedidos.Application.Pedidos.Dtos;
using GestaoPedidos.Application.Pedidos.Ports;
using GestaoPedidos.Application.Pedidos.Queries.ObterPedido;
using GestaoPedidos.Domain.Excecoes;
using NSubstitute;
using Shouldly;

namespace GestaoPedidos.UnitTests.Pedidos;

public sealed class ObterPedidoHandlerTests
{
    private readonly IPedidoQueries _consultas = Substitute.For<IPedidoQueries>();

    [Fact]
    public async Task Returns_the_order_detail_read_by_the_queries_port()
    {
        var detalhe = new PedidoDetalheDto(
            42,
            7,
            "Maria",
            new DateTime(2026, 3, 5, 10, 0, 0, DateTimeKind.Utc),
            31.50m,
            [new ItemPedidoDto(3, "Teclado", "SKU-TEC", 3, 10.50m, 31.50m)]);
        using var cancelamento = new CancellationTokenSource();
        _consultas.ObterAsync(42, cancelamento.Token).Returns(detalhe);

        var resultado = await new ObterPedidoHandler(_consultas).HandleAsync(new ObterPedidoQuery(42), cancelamento.Token);

        resultado.ShouldBeSameAs(detalhe);
        await _consultas.Received(1).ObterAsync(42, cancelamento.Token);
    }

    [Fact]
    public async Task Throws_order_not_found_with_the_requested_id_when_the_order_does_not_exist()
    {
        _consultas.ObterAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((PedidoDetalheDto?)null);

        var excecao = await Should.ThrowAsync<PedidoNaoEncontradoException>(
            () => new ObterPedidoHandler(_consultas).HandleAsync(new ObterPedidoQuery(999), CancellationToken.None));

        excecao.PedidoId.ShouldBe(999);
    }
}
