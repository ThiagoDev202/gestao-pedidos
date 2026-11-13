using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using GestaoPedidos.Application.Abstractions;
using GestaoPedidos.Application.Pedidos.Commands.CriarPedido;
using GestaoPedidos.Application.Pedidos.Dtos;
using GestaoPedidos.Application.Pedidos.Ports;
using GestaoPedidos.Domain.Excecoes;
using GestaoPedidos.Domain.Pedidos;
using GestaoPedidos.Domain.Produtos;
using NSubstitute;
using Shouldly;

namespace GestaoPedidos.UnitTests.Pedidos;

public sealed class CriarPedidoHandlerTests
{
    private const int ClienteId = 7;
    private const int PedidoIdGerado = 42;
    private static readonly DateTime Agora = new(2026, 2, 10, 9, 15, 0, 500, DateTimeKind.Utc);
    private static readonly Guid Chave = Guid.Parse("0b7f3c1e-9a52-4d0e-8f4b-2d6c1a9e7b30");

    private readonly IClienteRepository _clientes = Substitute.For<IClienteRepository>();
    private readonly IProdutoRepository _produtos = Substitute.For<IProdutoRepository>();
    private readonly IPedidoRepository _pedidos = Substitute.For<IPedidoRepository>();
    private readonly IIdempotenciaRepository _idempotencia = Substitute.For<IIdempotenciaRepository>();
    private readonly IPedidoQueries _consultas = Substitute.For<IPedidoQueries>();
    private readonly UnitOfWorkQueExecuta _unitOfWork = new();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly List<string> _chamadas = [];
    private readonly List<string> _chamadasForaDaTransacao = [];
    private readonly List<int> _baixas = [];
    private readonly Dictionary<int, Produto> _catalogo = [];
    private readonly HashSet<int> _semEstoque = [];
    private readonly Dictionary<int, PedidoDetalheDto> _detalhes = [];
    private Pedido? _pedidoAdicionado;

    public CriarPedidoHandlerTests()
    {
        _clock.UtcNow.Returns(Agora);

        _clientes.ExisteAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ci => Registrar("cliente", ci.ArgAt<int>(0) == ClienteId));

        _produtos.ObterPorIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(ci => Registrar<IReadOnlyList<Produto>>(
                "produtos",
                ci.ArgAt<IReadOnlyCollection<int>>(0).Where(_catalogo.ContainsKey).Select(id => _catalogo[id]).ToList()));

        _produtos.TentarBaixarEstoqueAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var produtoId = ci.ArgAt<int>(0);
                _baixas.Add(produtoId);
                return Registrar("baixa", !_semEstoque.Contains(produtoId));
            });

        _pedidos.AdicionarAsync(Arg.Any<Pedido>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var pedido = ci.ArgAt<Pedido>(0);
                typeof(Pedido).GetProperty(nameof(Pedido.Id), BindingFlags.Public | BindingFlags.Instance)!
                    .SetValue(pedido, PedidoIdGerado);
                _pedidoAdicionado = pedido;
                Registrar("adicionar", true);
                return Task.CompletedTask;
            });

        _idempotencia.AdquirirTravaAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                Registrar("trava", true);
                return Task.CompletedTask;
            });

        _idempotencia.ObterAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ => Registrar<RegistroIdempotencia?>("idempotencia", null));

        _idempotencia.RegistrarAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                Registrar("registrar", true);
                return Task.CompletedTask;
            });

        _consultas.ObterAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ci => (PedidoDetalheDto?)Detalhe(ci.ArgAt<int>(0)));

        AdicionarProduto(3, "Teclado", 150.00m, 10);
        AdicionarProduto(5, "Mouse", 49.90m, 1);
        AdicionarProduto(9, "Monitor", 899.99m, 4);
    }

    private CriarPedidoHandler Handler() =>
        new(_clientes, _produtos, _pedidos, _idempotencia, _consultas, _unitOfWork, _clock);

    private void AdicionarProduto(int id, string nome, decimal preco, int estoque)
    {
        _catalogo[id] = new Produto { Id = id, Nome = nome, Sku = "SKU-" + id, Preco = preco, Estoque = estoque };
    }

    private T Registrar<T>(string chamada, T resultado)
    {
        _chamadas.Add(chamada);
        if (!_unitOfWork.EmExecucao)
        {
            _chamadasForaDaTransacao.Add(chamada);
        }

        return resultado;
    }

    private PedidoDetalheDto Detalhe(int id)
    {
        if (!_detalhes.TryGetValue(id, out var detalhe))
        {
            detalhe = new PedidoDetalheDto(id, ClienteId, "Cliente", Agora, 1m, [new ItemPedidoDto(3, "Teclado", "SKU-3", 1, 1m, 1m)]);
            _detalhes[id] = detalhe;
        }

        return detalhe;
    }

    private static CriarPedidoCommand Comando(Guid? chave = null, int clienteId = ClienteId, params (int ProdutoId, int Quantidade)[] itens) =>
        new(clienteId, itens.Select(i => new ItemDoPedidoCommand(i.ProdutoId, i.Quantidade)).ToList(), chave);

    private static string Hash(int clienteId, params (int ProdutoId, int Quantidade)[] itens)
    {
        var normalizado = $"{clienteId}|" + string.Join(",", itens.OrderBy(i => i.ProdutoId).Select(i => $"{i.ProdutoId}:{i.Quantidade}"));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(normalizado)));
    }

    [Fact]
    public async Task Rejects_missing_customer_without_touching_stock()
    {
        var erro = await Should.ThrowAsync<ClienteInexistenteException>(() =>
            Handler().HandleAsync(Comando(null, 999, (3, 1)), CancellationToken.None));

        erro.ClienteId.ShouldBe(999);
        _baixas.ShouldBeEmpty();
        await _pedidos.DidNotReceiveWithAnyArgs().AdicionarAsync(default!, default);
    }

    [Fact]
    public async Task Rejects_missing_product_without_touching_stock()
    {
        var erro = await Should.ThrowAsync<ProdutoInexistenteException>(() =>
            Handler().HandleAsync(Comando(null, ClienteId, (3, 1), (404, 2)), CancellationToken.None));

        erro.ProdutoId.ShouldBe(404);
        _baixas.ShouldBeEmpty();
        await _pedidos.DidNotReceiveWithAnyArgs().AdicionarAsync(default!, default);
    }

    [Fact]
    public async Task Rejects_insufficient_stock_with_product_requested_and_available()
    {
        _semEstoque.Add(5);

        var erro = await Should.ThrowAsync<EstoqueInsuficienteException>(() =>
            Handler().HandleAsync(Comando(null, ClienteId, (5, 4), (3, 2)), CancellationToken.None));

        erro.ProdutoId.ShouldBe(5);
        erro.ProdutoNome.ShouldBe("Mouse");
        erro.Solicitado.ShouldBe(4);
        erro.Disponivel.ShouldBe(1);
        await _pedidos.DidNotReceiveWithAnyArgs().AdicionarAsync(default!, default);
        await _idempotencia.DidNotReceiveWithAnyArgs().RegistrarAsync(default, default!, default, default);
    }

    [Fact]
    public async Task Decrements_stock_in_ascending_product_id_order()
    {
        await Handler().HandleAsync(Comando(null, ClienteId, (9, 1), (3, 2), (5, 1)), CancellationToken.None);

        _baixas.ShouldBe([3, 5, 9]);
    }

    [Fact]
    public async Task Decrements_each_item_by_its_quantity()
    {
        await Handler().HandleAsync(Comando(null, ClienteId, (9, 3), (3, 2)), CancellationToken.None);

        await _produtos.Received(1).TentarBaixarEstoqueAsync(3, 2, Arg.Any<CancellationToken>());
        await _produtos.Received(1).TentarBaixarEstoqueAsync(9, 3, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Creates_order_with_current_product_prices_total_and_clock_date()
    {
        await Handler().HandleAsync(Comando(null, ClienteId, (9, 1), (3, 2)), CancellationToken.None);

        _pedidoAdicionado.ShouldNotBeNull();
        _pedidoAdicionado.ClienteId.ShouldBe(ClienteId);
        _pedidoAdicionado.DataPedido.ShouldBe(Agora);
        _pedidoAdicionado.Itens.Select(i => (i.ProdutoId, i.Quantidade, i.PrecoUnitario))
            .ShouldBe([(3, 2, 150.00m), (9, 1, 899.99m)], ignoreOrder: true);
        _pedidoAdicionado.ValorTotal.ShouldBe(1199.99m);
    }

    [Fact]
    public async Task Returns_the_created_order_read_back_by_its_id()
    {
        var resultado = await Handler().HandleAsync(Comando(null, ClienteId, (3, 1)), CancellationToken.None);

        resultado.Reenvio.ShouldBeFalse();
        resultado.Pedido.ShouldBeSameAs(Detalhe(PedidoIdGerado));
        await _consultas.Received(1).ObterAsync(PedidoIdGerado, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Runs_every_write_inside_a_single_unit_of_work()
    {
        await Handler().HandleAsync(Comando(Chave, ClienteId, (3, 1), (5, 1)), CancellationToken.None);

        _unitOfWork.Execucoes.ShouldBe(1);
        _chamadas.ShouldContain("adicionar");
        _chamadas.ShouldContain("registrar");
        _chamadasForaDaTransacao.ShouldBeEmpty();
    }

    [Fact]
    public async Task Does_not_use_idempotency_storage_without_a_key()
    {
        await Handler().HandleAsync(Comando(null, ClienteId, (3, 1)), CancellationToken.None);

        await _idempotencia.DidNotReceiveWithAnyArgs().AdquirirTravaAsync(default, default);
        await _idempotencia.DidNotReceiveWithAnyArgs().ObterAsync(default, default);
        await _idempotencia.DidNotReceiveWithAnyArgs().RegistrarAsync(default, default!, default, default);
    }

    [Fact]
    public async Task Takes_the_idempotency_lock_before_anything_else()
    {
        await Handler().HandleAsync(Comando(Chave, ClienteId, (3, 1)), CancellationToken.None);

        await _idempotencia.Received(1).AdquirirTravaAsync(Chave, Arg.Any<CancellationToken>());
        _chamadas.First().ShouldBe("trava");
    }

    [Fact]
    public async Task Takes_the_idempotency_lock_before_rejecting_a_missing_customer()
    {
        await Should.ThrowAsync<ClienteInexistenteException>(() =>
            Handler().HandleAsync(Comando(Chave, 999, (3, 1)), CancellationToken.None));

        _chamadas.First().ShouldBe("trava");
    }

    [Fact]
    public async Task Records_the_idempotency_key_with_the_normalized_body_hash_and_the_new_order()
    {
        await Handler().HandleAsync(Comando(Chave, ClienteId, (9, 1), (3, 2)), CancellationToken.None);

        await _idempotencia.Received(1).RegistrarAsync(Chave, Hash(ClienteId, (3, 2), (9, 1)), PedidoIdGerado, Arg.Any<CancellationToken>());
        _chamadas.IndexOf("registrar").ShouldBeGreaterThan(_chamadas.IndexOf("adicionar"));
    }

    [Fact]
    public async Task Returns_the_original_order_when_the_key_is_replayed_with_the_same_body()
    {
        _idempotencia.ObterAsync(Chave, Arg.Any<CancellationToken>())
            .Returns(_ => Registrar<RegistroIdempotencia?>("idempotencia", new RegistroIdempotencia(Chave, Hash(ClienteId, (3, 2), (9, 1)), 77)));

        var resultado = await Handler().HandleAsync(Comando(Chave, ClienteId, (9, 1), (3, 2)), CancellationToken.None);

        resultado.Reenvio.ShouldBeTrue();
        resultado.Pedido.ShouldBeSameAs(Detalhe(77));
        _baixas.ShouldBeEmpty();
        await _pedidos.DidNotReceiveWithAnyArgs().AdicionarAsync(default!, default);
        await _idempotencia.DidNotReceiveWithAnyArgs().RegistrarAsync(default, default!, default, default);
        _chamadas.First().ShouldBe("trava");
    }

    [Fact]
    public async Task Rejects_a_key_reused_with_a_different_body()
    {
        _idempotencia.ObterAsync(Chave, Arg.Any<CancellationToken>())
            .Returns(_ => Registrar<RegistroIdempotencia?>("idempotencia", new RegistroIdempotencia(Chave, Hash(ClienteId, (3, 1)), 77)));

        var erro = await Should.ThrowAsync<ChaveReutilizadaException>(() =>
            Handler().HandleAsync(Comando(Chave, ClienteId, (3, 2)), CancellationToken.None));

        erro.Chave.ShouldBe(Chave);
        _baixas.ShouldBeEmpty();
        await _pedidos.DidNotReceiveWithAnyArgs().AdicionarAsync(default!, default);
        await _idempotencia.DidNotReceiveWithAnyArgs().RegistrarAsync(default, default!, default, default);
    }

    [Fact]
    public async Task Rejects_a_key_reused_for_another_customer()
    {
        _idempotencia.ObterAsync(Chave, Arg.Any<CancellationToken>())
            .Returns(_ => Registrar<RegistroIdempotencia?>("idempotencia", new RegistroIdempotencia(Chave, Hash(8, (3, 1)), 77)));

        await Should.ThrowAsync<ChaveReutilizadaException>(() =>
            Handler().HandleAsync(Comando(Chave, ClienteId, (3, 1)), CancellationToken.None));

        _baixas.ShouldBeEmpty();
    }
}
