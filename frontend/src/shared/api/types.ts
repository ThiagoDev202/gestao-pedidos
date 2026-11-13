import { z } from 'zod'

export const erroDeCampoSchema = z.object({
  campo: z.string(),
  code: z.string(),
  mensagem: z.string(),
})

export const problemaSchema = z.object({
  type: z.string(),
  title: z.string(),
  status: z.number(),
  detail: z.string(),
  code: z.string(),
  traceId: z.string().optional(),
  meta: z.record(z.string(), z.unknown()).nullish(),
  errors: z.array(erroDeCampoSchema).nullish(),
})

export const clienteSchema = z.object({ id: z.number(), nome: z.string(), email: z.string() })

export const produtoSchema = z.object({
  id: z.number(),
  nome: z.string(),
  sku: z.string(),
  preco: z.number(),
  estoque: z.number(),
})

export const pedidoResumoSchema = z.object({
  id: z.number(),
  clienteId: z.number(),
  clienteNome: z.string(),
  dataPedido: z.string(),
  valorTotal: z.number(),
  quantidadeItens: z.number(),
})

export const pedidosPaginadosSchema = z.object({
  items: z.array(pedidoResumoSchema),
  page: z.number(),
  pageSize: z.number(),
  totalItems: z.number(),
  totalPages: z.number(),
})

export const itemPedidoSchema = z.object({
  produtoId: z.number(),
  produtoNome: z.string(),
  sku: z.string(),
  quantidade: z.number(),
  precoUnitario: z.number(),
  subtotal: z.number(),
})

export const pedidoDetalheSchema = z.object({
  id: z.number(),
  clienteId: z.number(),
  clienteNome: z.string(),
  dataPedido: z.string(),
  valorTotal: z.number(),
  itens: z.array(itemPedidoSchema),
})

export type ErroDeCampo = z.infer<typeof erroDeCampoSchema>
export type Problema = z.infer<typeof problemaSchema>
export type Cliente = z.infer<typeof clienteSchema>
export type Produto = z.infer<typeof produtoSchema>
export type PedidoResumo = z.infer<typeof pedidoResumoSchema>
export type PedidosPaginados = z.infer<typeof pedidosPaginadosSchema>
export type ItemPedido = z.infer<typeof itemPedidoSchema>
export type PedidoDetalhe = z.infer<typeof pedidoDetalheSchema>

export interface CriarPedidoRequest {
  clienteId: number
  itens: { produtoId: number; quantidade: number }[]
}
