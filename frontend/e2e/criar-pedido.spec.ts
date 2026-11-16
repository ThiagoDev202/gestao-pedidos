import { expect, test, type APIRequestContext } from '@playwright/test'
import {
  abrirNovoPedido,
  escolherOpcao,
  moeda,
  nomeExato,
  obter,
  primeiroItem,
  produtoPorId,
  type Cliente,
  type PedidoDetalhe,
  type PedidosPaginados,
  type Produto,
} from './apoio'

function hojeUtc(): string {
  return new Date().toISOString().slice(0, 10)
}

async function clienteSemPedidoPosteriorHoje(request: APIRequestContext, hoje: string): Promise<Cliente> {
  const clientes = await obter<Cliente[]>(request, '/api/clientes')
  const ocorrencias = new Map<string, number>()
  for (const cliente of clientes) ocorrencias.set(cliente.nome, (ocorrencias.get(cliente.nome) ?? 0) + 1)
  const candidatos = clientes.filter((cliente) => ocorrencias.get(cliente.nome) === 1).slice(0, 20)
  for (const cliente of candidatos) {
    const pagina = await obter<PedidosPaginados>(
      request,
      `/api/pedidos?clienteId=${cliente.id}&dataInicio=${hoje}&dataFim=${hoje}&pageSize=100`,
    )
    const agora = Date.now()
    if (pagina.items.every((pedido) => Date.parse(pedido.dataPedido) <= agora)) return cliente
  }
  throw new Error('nenhum cliente sem pedido posterior ao horário atual')
}

async function produtoComMaisEstoque(request: APIRequestContext): Promise<Produto> {
  const produtos = await obter<Produto[]>(request, '/api/produtos')
  const [produto] = [...produtos].filter((candidato) => candidato.estoque >= 10).sort((a, b) => b.estoque - a.estoque)
  if (!produto) throw new Error('nenhum produto com estoque no seed')
  return produto
}

test('creates an order through the dialog and shows it at the top of the list with the right total', async ({ page, request }) => {
  const hoje = hojeUtc()
  const cliente = await clienteSemPedidoPosteriorHoje(request, hoje)
  const produto = await produtoComMaisEstoque(request)

  await page.goto(`/?clienteId=${cliente.id}&dataInicio=${hoje}&dataFim=${hoje}`)
  await expect(page.getByRole('heading', { level: 1, name: 'Pedidos' })).toBeVisible()

  const dialogo = await abrirNovoPedido(page)
  await escolherOpcao(dialogo.getByRole('combobox', { name: 'Cliente', exact: true }), nomeExato(cliente.nome))
  const item = await primeiroItem(dialogo)
  await escolherOpcao(item.produto, nomeExato(produto.nome))
  await item.quantidade.fill('1')

  const [resposta] = await Promise.all([
    page.waitForResponse((r) => new URL(r.url()).pathname === '/api/pedidos' && r.request().method() === 'POST'),
    dialogo.getByRole('button', { name: 'Criar pedido' }).click(),
  ])

  expect(resposta.status()).toBe(201)
  const criado = (await resposta.json()) as PedidoDetalhe
  expect(criado.clienteId).toBe(cliente.id)
  expect(criado.valorTotal).toBeCloseTo(produto.preco, 2)
  await expect(dialogo).toBeHidden()

  const primeiraLinha = page.getByRole('table').getByRole('row').nth(1)
  await expect(primeiraLinha).toContainText(String(criado.id))
  await expect(primeiraLinha).toContainText(cliente.nome)
  await expect(primeiraLinha).toContainText(moeda(produto.preco))

  const depois = await produtoPorId(request, produto.id)
  expect(depois.estoque).toBe(produto.estoque - 1)
})
