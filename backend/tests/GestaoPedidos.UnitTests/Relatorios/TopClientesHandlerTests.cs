using GestaoPedidos.Application.Relatorios.Ports;
using GestaoPedidos.Application.Relatorios.Queries.TopClientes;
using NSubstitute;
using Shouldly;

namespace GestaoPedidos.UnitTests.Relatorios;

public sealed class TopClientesHandlerTests
{
    private readonly IRelatorioQueries _consultas = Substitute.For<IRelatorioQueries>();

    [Fact]
    public async Task Returns_the_ranking_read_for_the_requested_period()
    {
        var inicio = new DateOnly(2026, 3, 1);
        var fim = new DateOnly(2026, 3, 31);
        IReadOnlyList<TopClienteDto> ranking =
        [
            new TopClienteDto(3, "Ana", 700m, 3),
            new TopClienteDto(1, "Bruno", 500m, 2),
        ];
        using var cancelamento = new CancellationTokenSource();
        _consultas.TopClientesAsync(inicio, fim, cancelamento.Token).Returns(ranking);

        var resultado = await new TopClientesHandler(_consultas).HandleAsync(new TopClientesQuery(inicio, fim), cancelamento.Token);

        resultado.ShouldBe(ranking);
        await _consultas.Received(1).TopClientesAsync(inicio, fim, cancelamento.Token);
    }

    [Fact]
    public async Task Returns_an_empty_ranking_when_the_port_finds_no_customer()
    {
        var dia = new DateOnly(2026, 3, 10);
        _consultas.TopClientesAsync(dia, dia, Arg.Any<CancellationToken>()).Returns([]);

        var resultado = await new TopClientesHandler(_consultas).HandleAsync(new TopClientesQuery(dia, dia), CancellationToken.None);

        resultado.ShouldBeEmpty();
    }
}
