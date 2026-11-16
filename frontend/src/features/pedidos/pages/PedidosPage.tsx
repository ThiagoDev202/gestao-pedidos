import { CircleCheck, Plus } from 'lucide-react'
import { useRef, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { NovoPedidoDialog } from '@/features/pedidos/components/NovoPedidoDialog'
import { PedidosFilters, type ValoresDosFiltros } from '@/features/pedidos/components/PedidosFilters'
import { PedidosTable } from '@/features/pedidos/components/PedidosTable'
import { usePedidos } from '@/features/pedidos/hooks/usePedidos'
import { ApiError } from '@/shared/api/ApiError'
import { mensagemDoCampo } from '@/shared/api/error-messages'
import { Button } from '@/shared/components/ui/button'
import { ErrorAlert } from '@/shared/components/ui/ErrorAlert'
import { FieldError } from '@/shared/components/ui/FieldError'

const TAMANHO_DA_PAGINA = 20

function lerPagina(texto: string | null) {
  const valor = Number(texto)
  return Number.isInteger(valor) && valor >= 1 ? valor : 1
}

export function PedidosPage() {
  const [parametros, setParametros] = useSearchParams()
  const [novoPedidoAberto, setNovoPedidoAberto] = useState(false)
  const [pedidoCriado, setPedidoCriado] = useState<number | null>(null)
  const botaoNovoPedido = useRef<HTMLButtonElement>(null)

  const valores: ValoresDosFiltros = {
    clienteId: parametros.get('clienteId') ?? '',
    dataInicio: parametros.get('dataInicio') ?? '',
    dataFim: parametros.get('dataFim') ?? '',
  }
  const pagina = lerPagina(parametros.get('page'))
  const temFiltro = Boolean(valores.clienteId || valores.dataInicio || valores.dataFim)

  const consulta = usePedidos({
    clienteId: valores.clienteId || undefined,
    dataInicio: valores.dataInicio || undefined,
    dataFim: valores.dataFim || undefined,
    page: pagina,
    pageSize: TAMANHO_DA_PAGINA,
  })

  function atualizar(proximos: Partial<ValoresDosFiltros> & { page?: number }) {
    const novos = new URLSearchParams(parametros)
    for (const [chave, valor] of Object.entries(proximos)) {
      if (valor === undefined || valor === '' || (chave === 'page' && valor === 1)) novos.delete(chave)
      else novos.set(chave, String(valor))
    }
    setParametros(novos)
  }

  function alterarFiltros(parcial: Partial<ValoresDosFiltros>) {
    atualizar({ ...parcial, page: 1 })
  }

  function limpar() {
    setParametros(new URLSearchParams())
  }

  const erro = consulta.error instanceof ApiError ? consulta.error : null
  const resultado = consulta.data

  return (
    <main className="mx-auto flex max-w-5xl flex-col gap-6 p-6">
      <header className="flex items-center justify-between gap-4">
        <h1 className="text-2xl font-semibold">Pedidos</h1>
        <Button ref={botaoNovoPedido} onClick={() => { setPedidoCriado(null); setNovoPedidoAberto(true) }}>
          <Plus className="h-4 w-4" aria-hidden="true" />
          Novo pedido
        </Button>
      </header>

      {pedidoCriado !== null ? (
        <p role="status" className="flex items-center gap-2 rounded-md border border-success/40 bg-success/10 p-3 text-sm text-success">
          <CircleCheck className="h-4 w-4" aria-hidden="true" />
          Pedido {pedidoCriado} criado com sucesso.
        </p>
      ) : null}

      <PedidosFilters valores={valores} onAlterar={alterarFiltros} onLimpar={limpar} />

      {consulta.isPending ? (
        <p role="status" className="text-sm text-muted-foreground">
          Carregando pedidos…
        </p>
      ) : null}

      {erro ? (
        <div className="flex flex-col gap-2">
          <ErrorAlert erro={erro} onTentarNovamente={() => void consulta.refetch()} />
          {erro.errors.map((campo) => (
            <FieldError key={`${campo.campo}-${campo.code}`} mensagem={mensagemDoCampo(campo)} />
          ))}
        </div>
      ) : null}

      {resultado && resultado.items.length === 0 ? (
        <div className="flex flex-col items-center gap-3 rounded-lg border border-dashed p-10 text-center">
          <p className="font-medium">Nenhum pedido encontrado</p>
          {temFiltro ? (
            <Button variant="outline" onClick={limpar}>
              Limpar filtros
            </Button>
          ) : pagina > 1 ? (
            <Button variant="outline" onClick={() => atualizar({ page: 1 })}>
              Voltar à primeira página
            </Button>
          ) : null}
        </div>
      ) : null}

      {resultado && resultado.items.length > 0 ? (
        <section className="flex flex-col gap-4">
          <PedidosTable pedidos={resultado.items} />
          <nav aria-label="Paginação" className="flex items-center justify-between gap-4 text-sm">
            <span>
              Página {resultado.page} de {resultado.totalPages} · {resultado.totalItems} {resultado.totalItems === 1 ? 'pedido' : 'pedidos'}
            </span>
            <div className="flex gap-2">
              <Button variant="outline" size="sm" disabled={resultado.page <= 1} onClick={() => atualizar({ page: resultado.page - 1 })}>
                Anterior
              </Button>
              <Button
                variant="outline"
                size="sm"
                disabled={resultado.page >= resultado.totalPages}
                onClick={() => atualizar({ page: resultado.page + 1 })}
              >
                Próxima
              </Button>
            </div>
          </nav>
        </section>
      ) : null}

      <NovoPedidoDialog
        open={novoPedidoAberto}
        onOpenChange={setNovoPedidoAberto}
        onCriado={(pedido) => setPedidoCriado(pedido.id)}
        retornarFoco={botaoNovoPedido}
      />
    </main>
  )
}
