import { expect, type APIRequestContext, type Locator, type Page } from '@playwright/test'

export interface Cliente {
  id: number
  nome: string
}

export interface Produto {
  id: number
  nome: string
  preco: number
  estoque: number
}

export interface PedidoResumo {
  id: number
  clienteId: number
  dataPedido: string
  valorTotal: number
}

export interface PedidosPaginados {
  items: PedidoResumo[]
  totalItems: number
}

export interface PedidoDetalhe {
  id: number
  clienteId: number
  dataPedido: string
  valorTotal: number
}

export async function obter<T>(request: APIRequestContext, caminho: string): Promise<T> {
  const resposta = await request.get(caminho)
  expect(resposta.ok(), `GET ${caminho} respondeu ${resposta.status()}`).toBeTruthy()
  return (await resposta.json()) as T
}

export async function produtoPorId(request: APIRequestContext, id: number): Promise<Produto> {
  const produtos = await obter<Produto[]>(request, '/api/produtos')
  const produto = produtos.find((candidato) => candidato.id === id)
  if (!produto) throw new Error(`produto ${id} ausente no seed`)
  return produto
}

const formatoMoeda = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })

export function moeda(valor: number): string {
  return formatoMoeda.format(valor).replace(/\s+/g, ' ')
}

function escaparRegex(texto: string): string {
  return texto.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
}

export function nomeExato(nome: string): RegExp {
  return new RegExp(`(?<![\\w])${escaparRegex(nome)}(?![\\w])`)
}

export async function escolherOpcao(select: Locator, texto: RegExp) {
  const opcao = select.locator('option').filter({ hasText: texto }).first()
  await expect(opcao).toBeAttached()
  const valor = await opcao.getAttribute('value')
  expect(valor, `opção ${texto} sem value`).toBeTruthy()
  await select.selectOption(valor ?? '')
}

export async function abrirNovoPedido(page: Page): Promise<Locator> {
  await page.getByRole('button', { name: 'Novo pedido' }).click()
  const dialogo = page.getByRole('dialog', { name: 'Novo pedido' })
  await expect(dialogo).toBeVisible()
  return dialogo
}

export async function primeiroItem(dialogo: Locator): Promise<{ produto: Locator; quantidade: Locator }> {
  const produtos = dialogo.getByRole('combobox', { name: /^Produto/ })
  if ((await produtos.count()) === 0) await dialogo.getByRole('button', { name: 'Adicionar item' }).click()
  return {
    produto: produtos.first(),
    quantidade: dialogo.getByRole('spinbutton', { name: /^Quantidade/ }).first(),
  }
}
