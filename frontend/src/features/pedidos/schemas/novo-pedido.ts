import { z } from 'zod'

export const MAXIMO_DE_ITENS = 50

const itemSchema = z.object({
  produtoId: z.string().min(1, 'Selecione um produto.'),
  quantidade: z.string().regex(/^[1-9]\d*$/, 'Informe uma quantidade inteira maior que zero.'),
})

export const novoPedidoSchema = z.object({
  clienteId: z.string().min(1, 'Selecione um cliente.'),
  itens: z
    .array(itemSchema)
    .min(1, 'Adicione pelo menos um item ao pedido.')
    .max(MAXIMO_DE_ITENS, `Um pedido pode ter no máximo ${MAXIMO_DE_ITENS} itens.`)
    .superRefine((itens, contexto) => {
      const vistos = new Set<string>()
      itens.forEach((item, indice) => {
        if (!item.produtoId) return
        if (vistos.has(item.produtoId)) {
          contexto.addIssue({ code: 'custom', path: [indice, 'produtoId'], message: 'Este produto já está no pedido.' })
        }
        vistos.add(item.produtoId)
      })
    }),
})

export type NovoPedidoFormulario = z.infer<typeof novoPedidoSchema>

export const formularioVazio: NovoPedidoFormulario = {
  clienteId: '',
  itens: [{ produtoId: '', quantidade: '1' }],
}
