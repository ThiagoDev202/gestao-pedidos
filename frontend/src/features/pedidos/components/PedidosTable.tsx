import type { PedidoResumo } from '@/shared/api/types'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/shared/components/ui/table'
import { formatarDataHora, formatarMoeda } from '@/shared/lib/format'

export function PedidosTable({ pedidos }: { pedidos: PedidoResumo[] }) {
  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Pedido</TableHead>
          <TableHead>Cliente</TableHead>
          <TableHead>Data</TableHead>
          <TableHead className="text-right">Itens</TableHead>
          <TableHead className="text-right">Total</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {pedidos.map((pedido) => (
          <TableRow key={pedido.id}>
            <TableCell>{pedido.id}</TableCell>
            <TableCell>{pedido.clienteNome}</TableCell>
            <TableCell>{formatarDataHora(pedido.dataPedido)}</TableCell>
            <TableCell className="text-right">{pedido.quantidadeItens}</TableCell>
            <TableCell className="text-right">{formatarMoeda(pedido.valorTotal)}</TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  )
}
