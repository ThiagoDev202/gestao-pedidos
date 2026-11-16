import { expect, test } from '@playwright/test'
import { abrirNovoPedido, escolherOpcao, nomeExato, obter, primeiroItem, produtoPorId, type Cliente, type PedidosPaginados } from './apoio'

test('keeps the dialog open naming the product and the available stock when stock is insufficient', async ({ page, request }) => {
  const produto = await produtoPorId(request, 1)
  const solicitado = produto.estoque + 5
  const [cliente] = await obter<Cliente[]>(request, '/api/clientes')
  if (!cliente) throw new Error('nenhum cliente no seed')
  const antes = await obter<PedidosPaginados>(request, '/api/pedidos?pageSize=1')

  await page.goto('/')
  const dialogo = await abrirNovoPedido(page)
  await escolherOpcao(dialogo.getByRole('combobox', { name: 'Cliente', exact: true }), nomeExato(cliente.nome))
  const item = await primeiroItem(dialogo)
  await escolherOpcao(item.produto, nomeExato(produto.nome))
  await item.quantidade.fill(String(solicitado))

  const [resposta] = await Promise.all([
    page.waitForResponse((r) => new URL(r.url()).pathname === '/api/pedidos' && r.request().method() === 'POST'),
    dialogo.getByRole('button', { name: 'Criar pedido' }).click(),
  ])

  expect(resposta.status()).toBe(422)
  expect(((await resposta.json()) as { code: string }).code).toBe('estoque_insuficiente')
  await expect(dialogo).toBeVisible()
  const alerta = dialogo.getByRole('alert').filter({ hasText: produto.nome })
  await expect(alerta).toBeVisible()
  await expect(alerta).toContainText(new RegExp(`\\b${produto.estoque}\\b`))
  await expect(alerta).toContainText(new RegExp(`\\b${solicitado}\\b`))
  await expect(item.quantidade).toHaveValue(String(solicitado))
  await expect(dialogo.getByRole('button', { name: 'Criar pedido' })).toBeEnabled()

  const depois = await obter<PedidosPaginados>(request, '/api/pedidos?pageSize=1')
  expect(depois.totalItems).toBe(antes.totalItems)
  expect((await produtoPorId(request, 1)).estoque).toBe(produto.estoque)
})
