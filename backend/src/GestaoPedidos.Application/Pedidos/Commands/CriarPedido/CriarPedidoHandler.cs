using System.Security.Cryptography;
using System.Text;
using GestaoPedidos.Application.Abstractions;
using GestaoPedidos.Application.Pedidos.Ports;
using GestaoPedidos.Domain.Excecoes;
using GestaoPedidos.Domain.Pedidos;

namespace GestaoPedidos.Application.Pedidos.Commands.CriarPedido;

public sealed class CriarPedidoHandler(
    IClienteRepository clientes,
    IProdutoRepository produtos,
    IPedidoRepository pedidos,
    IIdempotenciaRepository idempotencia,
    IPedidoQueries consultas,
    IUnitOfWork unitOfWork,
    IClock clock) : ICommandHandler<CriarPedidoCommand, CriarPedidoResult>
{
    public async Task<CriarPedidoResult> HandleAsync(CriarPedidoCommand command, CancellationToken cancellationToken)
    {
        var hash = CalcularHash(command);

        var (pedidoId, reenvio) = await unitOfWork.ExecuteAsync(
            ct => ProcessarAsync(command, hash, ct),
            cancellationToken);

        var detalhe = await consultas.ObterAsync(pedidoId, cancellationToken)
            ?? throw new PedidoNaoEncontradoException(pedidoId);

        return new CriarPedidoResult(detalhe, reenvio);
    }

    internal static string CalcularHash(CriarPedidoCommand command)
    {
        var itens = string.Join(',', command.Itens.OrderBy(i => i.ProdutoId).Select(i => $"{i.ProdutoId}:{i.Quantidade}"));
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{command.ClienteId}|{itens}"));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private async Task<(int PedidoId, bool Reenvio)> ProcessarAsync(CriarPedidoCommand command, string hash, CancellationToken ct)
    {
        if (command.ChaveIdempotencia is { } chave)
        {
            await idempotencia.AdquirirTravaAsync(chave, ct);
            var registro = await idempotencia.ObterAsync(chave, ct);
            if (registro is not null)
            {
                if (registro.HashCorpo != hash)
                {
                    throw new ChaveReutilizadaException(chave);
                }

                return (registro.PedidoId, true);
            }
        }

        if (!await clientes.ExisteAsync(command.ClienteId, ct))
        {
            throw new ClienteInexistenteException(command.ClienteId);
        }

        var ordenados = command.Itens.OrderBy(i => i.ProdutoId).ToList();
        var encontrados = await produtos.ObterPorIdsAsync(ordenados.Select(i => i.ProdutoId).ToList(), ct);
        var porId = encontrados.ToDictionary(p => p.Id);

        foreach (var item in ordenados)
        {
            if (!porId.ContainsKey(item.ProdutoId))
            {
                throw new ProdutoInexistenteException(item.ProdutoId);
            }
        }

        foreach (var item in ordenados)
        {
            if (!await produtos.TentarBaixarEstoqueAsync(item.ProdutoId, item.Quantidade, ct))
            {
                var produto = porId[item.ProdutoId];
                throw new EstoqueInsuficienteException(produto.Id, produto.Nome, item.Quantidade, produto.Estoque);
            }
        }

        var itensDoPedido = ordenados
            .Select(i => new ItemParaPedido(i.ProdutoId, i.Quantidade, porId[i.ProdutoId].Preco))
            .ToList();
        var pedido = Pedido.Criar(command.ClienteId, itensDoPedido, clock.UtcNow);

        await pedidos.AdicionarAsync(pedido, ct);

        if (command.ChaveIdempotencia is { } chaveNova)
        {
            await idempotencia.RegistrarAsync(chaveNova, hash, pedido.Id, ct);
        }

        return (pedido.Id, false);
    }
}
