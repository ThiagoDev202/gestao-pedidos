import type { Cliente, ErroDeCampo, PedidoDetalhe, PedidoResumo, PedidosPaginados, Problema, Produto } from '@/shared/api/types'

export function umCliente(dados: Partial<Cliente> = {}): Cliente {
  return { id: 1, nome: 'Ana Silva', email: 'ana.silva@exemplo.com', ...dados }
}

export function umProduto(dados: Partial<Produto> = {}): Produto {
  return { id: 10, nome: 'Teclado Mecânico', sku: 'SKU-0010', preco: 150.5, estoque: 8, ...dados }
}

export function umPedidoResumo(dados: Partial<PedidoResumo> = {}): PedidoResumo {
  return {
    id: 1001,
    clienteId: 1,
    clienteNome: 'Ana Silva',
    dataPedido: '2026-03-15T14:30:00Z',
    valorTotal: 301,
    quantidadeItens: 2,
    ...dados,
  }
}

export function umaPagina(itens: PedidoResumo[], dados: Partial<Omit<PedidosPaginados, 'items'>> = {}): PedidosPaginados {
  const pageSize = dados.pageSize ?? 20
  const totalItems = dados.totalItems ?? itens.length
  return {
    items: itens,
    page: dados.page ?? 1,
    pageSize,
    totalItems,
    totalPages: dados.totalPages ?? Math.ceil(totalItems / pageSize),
  }
}

export function umPedidoDetalhe(dados: Partial<PedidoDetalhe> = {}): PedidoDetalhe {
  return {
    id: 5001,
    clienteId: 2,
    clienteNome: 'Bruno Souza',
    dataPedido: '2026-11-13T12:00:00Z',
    valorTotal: 1200.9,
    itens: [
      { produtoId: 10, produtoNome: 'Teclado Mecânico', sku: 'SKU-0010', quantidade: 2, precoUnitario: 150.5, subtotal: 301 },
      { produtoId: 12, produtoNome: 'Monitor 24', sku: 'SKU-0012', quantidade: 1, precoUnitario: 899.9, subtotal: 899.9 },
    ],
    ...dados,
  }
}

const titulos: Record<string, string> = {
  validacao_falhou: 'Dados inválidos',
  pedido_nao_encontrado: 'Pedido não encontrado',
  recurso_nao_encontrado: 'Recurso não encontrado',
  formato_nao_suportado: 'Formato não suportado',
  cliente_inexistente: 'Cliente inexistente',
  produto_inexistente: 'Produto inexistente',
  estoque_insuficiente: 'Estoque insuficiente',
  chave_reutilizada: 'Chave de idempotência reutilizada',
  limite_requisicoes_excedido: 'Limite de requisições excedido',
  erro_interno: 'Erro interno',
  banco_indisponivel: 'Banco de dados indisponível',
}

export function umProblema(
  status: number,
  code: string,
  dados: { detail?: string; traceId?: string; meta?: Record<string, unknown>; errors?: ErroDeCampo[] } = {},
): Problema {
  return {
    type: `urn:gestaopedidos:erro:${code}`,
    title: titulos[code] ?? 'Erro',
    status,
    detail: dados.detail ?? 'Detalhe da ocorrência.',
    code,
    traceId: dados.traceId ?? '00-4bf92f3577b34da6a3ce929d0e0e4736-00',
    meta: dados.meta ?? null,
    errors: dados.errors ?? null,
  }
}

const formatoMoeda = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })

export function moeda(valor: number): string {
  return formatoMoeda.format(valor).replace(/\s+/g, ' ')
}

function doisDigitos(valor: number): string {
  return String(valor).padStart(2, '0')
}

export function dataLocal(utc: string): string {
  const data = new Date(utc)
  return `${doisDigitos(data.getDate())}/${doisDigitos(data.getMonth() + 1)}/${data.getFullYear()}`
}

export function horaLocal(utc: string): string {
  const data = new Date(utc)
  return `${doisDigitos(data.getHours())}:${doisDigitos(data.getMinutes())}`
}

export const formatoUuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i
