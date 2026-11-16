import { requisitar } from '@/shared/api/http-client'
import {
  clienteSchema,
  pedidoDetalheSchema,
  pedidosPaginadosSchema,
  produtoSchema,
  type CriarPedidoRequest,
} from '@/shared/api/types'
import { z } from 'zod'

export interface FiltrosDePedidos {
  clienteId?: string
  dataInicio?: string
  dataFim?: string
  page: number
  pageSize: number
}

export function listarPedidos(filtros: FiltrosDePedidos, signal?: AbortSignal) {
  return requisitar('/pedidos', { parametros: { ...filtros }, schema: pedidosPaginadosSchema, signal })
}

export function listarClientes() {
  return requisitar('/clientes', { schema: z.array(clienteSchema) })
}

export function listarProdutos() {
  return requisitar('/produtos', { schema: z.array(produtoSchema) })
}

export function criarPedido(pedido: CriarPedidoRequest, chaveDeIdempotencia: string) {
  return requisitar('/pedidos', {
    metodo: 'POST',
    corpo: pedido,
    cabecalhos: { 'Idempotency-Key': chaveDeIdempotencia },
    schema: pedidoDetalheSchema,
  })
}
