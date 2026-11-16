import { useQuery } from '@tanstack/react-query'
import { listarClientes, listarPedidos, listarProdutos, type FiltrosDePedidos } from '@/features/pedidos/api/pedidos'

export function usePedidos(filtros: FiltrosDePedidos) {
  return useQuery({ queryKey: ['pedidos', filtros], queryFn: ({ signal }) => listarPedidos(filtros, signal) })
}

export function useClientes() {
  return useQuery({ queryKey: ['clientes'], queryFn: listarClientes, staleTime: 60_000 })
}

export function useProdutos(ativo = true) {
  return useQuery({ queryKey: ['produtos'], queryFn: listarProdutos, enabled: ativo, staleTime: 0 })
}
