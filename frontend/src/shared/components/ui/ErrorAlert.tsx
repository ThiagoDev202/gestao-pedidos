import { CircleAlert } from 'lucide-react'
import type { ApiError } from '@/shared/api/ApiError'
import { mensagemDoErro } from '@/shared/api/error-messages'
import { Button } from '@/shared/components/ui/button'

const CODIGOS_CONHECIDOS_REPETIVEIS = new Set(['sem_conexao', 'limite_requisicoes_excedido', 'erro_interno', 'banco_indisponivel'])

function ehRepetivel(erro: ApiError) {
  return CODIGOS_CONHECIDOS_REPETIVEIS.has(erro.code) || erro.status === 429 || erro.status >= 500
}

function ehInesperado(erro: ApiError) {
  return erro.status >= 500 || erro.code === 'erro_interno' || !CODIGOS_DO_CATALOGO.has(erro.code)
}

const CODIGOS_DO_CATALOGO = new Set([
  'validacao_falhou',
  'pedido_nao_encontrado',
  'recurso_nao_encontrado',
  'formato_nao_suportado',
  'cliente_inexistente',
  'produto_inexistente',
  'estoque_insuficiente',
  'chave_reutilizada',
  'limite_requisicoes_excedido',
  'sem_conexao',
  'resposta_invalida',
])

interface ErrorAlertProps {
  erro: ApiError | null | undefined
  onTentarNovamente?: () => void
}

export function ErrorAlert({ erro, onTentarNovamente }: ErrorAlertProps) {
  if (!erro) return null
  return (
    <div role="alert" className="flex items-start gap-3 rounded-md border border-destructive/40 bg-destructive/10 p-4 text-sm text-destructive">
      <CircleAlert className="mt-0.5 h-4 w-4 shrink-0" aria-hidden="true" />
      <div className="flex flex-1 flex-col gap-2">
        <p>{mensagemDoErro(erro)}</p>
        {ehInesperado(erro) && erro.traceId ? <p className="text-xs">Código de suporte: {erro.traceId}</p> : null}
        {onTentarNovamente && ehRepetivel(erro) ? (
          <div>
            <Button variant="outline" size="sm" onClick={onTentarNovamente}>
              Tentar de novo
            </Button>
          </div>
        ) : null}
      </div>
    </div>
  )
}
