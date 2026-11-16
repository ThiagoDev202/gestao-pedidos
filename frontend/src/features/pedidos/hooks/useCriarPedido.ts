import { useMutation, useQueryClient } from '@tanstack/react-query'
import { criarPedido } from '@/features/pedidos/api/pedidos'
import type { CriarPedidoRequest } from '@/shared/api/types'

export function useCriarPedido() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (pedido: CriarPedidoRequest) => criarPedido(pedido, crypto.randomUUID()),
    onSuccess: async () => {
      await Promise.all([queryClient.invalidateQueries({ queryKey: ['pedidos'] }), queryClient.invalidateQueries({ queryKey: ['produtos'] })])
    },
  })
}
