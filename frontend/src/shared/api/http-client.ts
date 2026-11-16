import type { ZodType } from 'zod'
import { ApiError } from '@/shared/api/ApiError'
import { problemaSchema } from '@/shared/api/types'

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '/api'

export interface OpcoesDeRequisicao<T> {
  metodo?: 'GET' | 'POST'
  corpo?: unknown
  cabecalhos?: Record<string, string>
  parametros?: Record<string, string | number | undefined>
  schema?: ZodType<T>
  signal?: AbortSignal
}

function montarUrl(caminho: string, parametros: OpcoesDeRequisicao<unknown>['parametros']) {
  const consulta = new URLSearchParams()
  for (const [chave, valor] of Object.entries(parametros ?? {})) {
    if (valor !== undefined && valor !== '') consulta.set(chave, String(valor))
  }
  const texto = consulta.toString()
  return `${BASE_URL}${caminho}${texto ? `?${texto}` : ''}`
}

function respostaInvalida(status: number, traceId?: string) {
  return new ApiError({
    status,
    code: 'resposta_invalida',
    title: 'Resposta inválida',
    detail: 'A resposta do servidor não está no formato esperado.',
    traceId,
  })
}

async function lerJson(resposta: Response): Promise<unknown> {
  try {
    return await resposta.json()
  } catch {
    return undefined
  }
}

function lerEspera(resposta: Response): number | undefined {
  const bruto = resposta.headers.get('Retry-After')
  const valor = bruto === null ? Number.NaN : Number(bruto)
  return Number.isFinite(valor) ? valor : undefined
}

export async function requisitar<T>(caminho: string, opcoes: OpcoesDeRequisicao<T> = {}): Promise<T> {
  const { metodo = 'GET', corpo, cabecalhos = {}, parametros, schema, signal } = opcoes
  const headers: Record<string, string> = { Accept: 'application/json', ...cabecalhos }
  if (corpo !== undefined) headers['Content-Type'] = 'application/json'

  let resposta: Response
  try {
    resposta = await fetch(montarUrl(caminho, parametros), {
      method: metodo,
      headers,
      body: corpo === undefined ? undefined : JSON.stringify(corpo),
      signal,
    })
  } catch (causa) {
    if (causa instanceof DOMException && causa.name === 'AbortError') throw causa
    throw new ApiError({
      status: 0,
      code: 'sem_conexao',
      title: 'Sem conexão',
      detail: 'Não foi possível conectar ao servidor.',
    })
  }

  const json = await lerJson(resposta)

  if (!resposta.ok) {
    const problema = problemaSchema.safeParse(json)
    if (!problema.success) throw respostaInvalida(resposta.status)
    const { title, status, detail, code, traceId, meta, errors } = problema.data
    throw new ApiError({ title, status, detail, code, traceId, meta, errors, retryAfterSegundos: lerEspera(resposta) })
  }

  if (json === undefined) throw respostaInvalida(resposta.status)
  if (!schema) return json as T
  const lido = schema.safeParse(json)
  if (!lido.success) throw respostaInvalida(resposta.status)
  return lido.data
}
