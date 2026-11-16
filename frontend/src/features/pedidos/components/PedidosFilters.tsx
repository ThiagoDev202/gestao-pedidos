import { useClientes } from '@/features/pedidos/hooks/usePedidos'
import { Button } from '@/shared/components/ui/button'
import { Input } from '@/shared/components/ui/input'
import { Label } from '@/shared/components/ui/label'
import { Select } from '@/shared/components/ui/select'

export interface ValoresDosFiltros {
  clienteId: string
  dataInicio: string
  dataFim: string
}

interface PedidosFiltersProps {
  valores: ValoresDosFiltros
  onAlterar: (parcial: Partial<ValoresDosFiltros>) => void
  onLimpar: () => void
}

export function PedidosFilters({ valores, onAlterar, onLimpar }: PedidosFiltersProps) {
  const { data: clientes = [] } = useClientes()

  return (
    <div className="flex flex-wrap items-end gap-4">
      <div className="flex min-w-48 flex-col gap-1.5">
        <Label htmlFor="filtro-cliente">Cliente</Label>
        <Select id="filtro-cliente" value={valores.clienteId} onChange={(evento) => onAlterar({ clienteId: evento.target.value })}>
          <option value="">Todos os clientes</option>
          {clientes.map((cliente) => (
            <option key={cliente.id} value={cliente.id}>
              {cliente.nome}
            </option>
          ))}
        </Select>
      </div>
      <div className="flex flex-col gap-1.5">
        <Label htmlFor="filtro-data-inicial">Data inicial</Label>
        <Input
          id="filtro-data-inicial"
          type="date"
          value={valores.dataInicio}
          onChange={(evento) => onAlterar({ dataInicio: evento.target.value })}
        />
      </div>
      <div className="flex flex-col gap-1.5">
        <Label htmlFor="filtro-data-final">Data final</Label>
        <Input id="filtro-data-final" type="date" value={valores.dataFim} onChange={(evento) => onAlterar({ dataFim: evento.target.value })} />
      </div>
      <Button variant="outline" onClick={onLimpar}>
        Limpar filtros
      </Button>
    </div>
  )
}
