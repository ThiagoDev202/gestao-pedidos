import { vi } from 'vitest'

export interface RequisicaoCapturada {
  url: URL
  metodo: string
  cabecalhos: Headers
  corpo: unknown
}

export type Rota = (requisicao: RequisicaoCapturada) => Response | Promise<Response>

export interface FetchFalso {
  chamadas: RequisicaoCapturada[]
  chamadasDe: (metodo: string, caminho: string) => RequisicaoCapturada[]
  definir: (metodo: string, caminho: string, rota: Rota) => void
}

function lerCorpo(corpo: BodyInit | null | undefined): unknown {
  if (typeof corpo !== 'string') return corpo ?? undefined
  try {
    return JSON.parse(corpo)
  } catch {
    return corpo
  }
}

export function instalarFetchFalso(rotasIniciais: Record<string, Rota> = {}): FetchFalso {
  const rotas = new Map<string, Rota>(Object.entries(rotasIniciais))
  const chamadas: RequisicaoCapturada[] = []

  const falso = vi.fn(async (entrada: RequestInfo | URL, init?: RequestInit) => {
    const ehRequest = typeof Request !== 'undefined' && entrada instanceof Request
    const endereco = ehRequest ? entrada.url : String(entrada)
    const url = new URL(endereco, 'http://localhost')
    const metodo = (init?.method ?? (ehRequest ? entrada.method : 'GET')).toUpperCase()
    const cabecalhos = new Headers(init?.headers ?? (ehRequest ? entrada.headers : undefined))
    const corpo = ehRequest && init?.body === undefined ? lerCorpo(await entrada.clone().text()) : lerCorpo(init?.body)
    const requisicao: RequisicaoCapturada = { url, metodo, cabecalhos, corpo }
    chamadas.push(requisicao)
    const rota = rotas.get(`${metodo} ${url.pathname}`)
    if (!rota) return respostaJson(404, { mensagem: `rota não simulada: ${metodo} ${url.pathname}` })
    return rota(requisicao)
  })

  vi.stubGlobal('fetch', falso)

  return {
    chamadas,
    chamadasDe: (metodo, caminho) => chamadas.filter((c) => c.metodo === metodo && c.url.pathname === caminho),
    definir: (metodo, caminho, rota) => {
      rotas.set(`${metodo} ${caminho}`, rota)
    },
  }
}

export function respostaJson(status: number, corpo: unknown, cabecalhos: Record<string, string> = {}): Response {
  return new Response(JSON.stringify(corpo), {
    status,
    headers: { 'Content-Type': 'application/json; charset=utf-8', ...cabecalhos },
  })
}

export function respostaProblema(status: number, corpo: unknown, cabecalhos: Record<string, string> = {}): Response {
  return new Response(JSON.stringify(corpo), {
    status,
    headers: { 'Content-Type': 'application/problem+json; charset=utf-8', ...cabecalhos },
  })
}

export function falhaDeRede(): Promise<Response> {
  return Promise.reject(new TypeError('Failed to fetch'))
}

export function pendente(): { promessa: Promise<Response>; resolver: (resposta: Response) => void } {
  let resolver: (resposta: Response) => void = () => undefined
  const promessa = new Promise<Response>((resolve) => {
    resolver = resolve
  })
  return { promessa, resolver }
}
