import { z } from 'zod'
import { ApiError } from '@/shared/api/ApiError'
import { requisitar } from '@/shared/api/http-client'
import { clienteSchema, pedidosPaginadosSchema } from '@/shared/api/types'
import { umCliente, umaPagina, umPedidoResumo, umProblema } from '@/test/builders'
import { falhaDeRede, instalarFetchFalso, respostaJson, respostaProblema } from '@/test/http'

async function capturarErro(promessa: Promise<unknown>): Promise<ApiError> {
  try {
    await promessa
  } catch (erro) {
    expect(erro).toBeInstanceOf(ApiError)
    return erro as ApiError
  }
  throw new Error('a requisição deveria ter falhado')
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('ApiError', () => {
  it('is an Error carrying the problem fields', () => {
    const erro = new ApiError({
      status: 422,
      code: 'estoque_insuficiente',
      title: 'Estoque insuficiente',
      detail: 'Sem estoque.',
      meta: { produtoId: 12 },
      errors: [{ campo: 'itens[0].quantidade', code: 'quantidade_invalida', mensagem: 'Inválida.' }],
      traceId: 'trace-1',
      retryAfterSegundos: 3,
    })

    expect(erro).toBeInstanceOf(Error)
    expect(erro.status).toBe(422)
    expect(erro.code).toBe('estoque_insuficiente')
    expect(erro.title).toBe('Estoque insuficiente')
    expect(erro.detail).toBe('Sem estoque.')
    expect(erro.meta).toEqual({ produtoId: 12 })
    expect(erro.errors).toHaveLength(1)
    expect(erro.traceId).toBe('trace-1')
    expect(erro.retryAfterSegundos).toBe(3)
  })

  it('defaults meta to an empty object and errors to an empty list', () => {
    const erro = new ApiError({ status: 500, code: 'erro_interno', title: 'Erro interno', detail: 'Falhou.' })

    expect(erro.meta).toEqual({})
    expect(erro.errors).toEqual([])
    expect(erro.traceId).toBeUndefined()
  })
})

describe('requisitar', () => {
  it('prefixes the path with /api and returns the body parsed by the schema', async () => {
    const pagina = umaPagina([umPedidoResumo()])
    const fetchFalso = instalarFetchFalso({ 'GET /api/pedidos': () => respostaJson(200, pagina) })

    const resultado = await requisitar('/pedidos', { schema: pedidosPaginadosSchema })

    expect(resultado).toEqual(pagina)
    expect(fetchFalso.chamadas).toHaveLength(1)
    expect(fetchFalso.chamadas[0]?.url.pathname).toBe('/api/pedidos')
    expect(fetchFalso.chamadas[0]?.metodo).toBe('GET')
  })

  it('sends parameters in the query string and skips undefined and empty values', async () => {
    const fetchFalso = instalarFetchFalso({ 'GET /api/pedidos': () => respostaJson(200, umaPagina([])) })

    await requisitar('/pedidos', {
      parametros: { page: 2, pageSize: 20, clienteId: undefined, dataInicio: '', dataFim: '2026-01-31' },
      schema: pedidosPaginadosSchema,
    })

    const parametros = fetchFalso.chamadas[0]?.url.searchParams
    expect(parametros?.get('page')).toBe('2')
    expect(parametros?.get('pageSize')).toBe('20')
    expect(parametros?.get('dataFim')).toBe('2026-01-31')
    expect(parametros?.has('clienteId')).toBe(false)
    expect(parametros?.has('dataInicio')).toBe(false)
  })

  it('posts the body as JSON with the given headers', async () => {
    const fetchFalso = instalarFetchFalso({ 'POST /api/pedidos': () => respostaJson(201, umCliente()) })

    await requisitar('/pedidos', {
      metodo: 'POST',
      corpo: { clienteId: 2, itens: [{ produtoId: 10, quantidade: 1 }] },
      cabecalhos: { 'Idempotency-Key': '3f1c2a4e-8b7d-4c6a-9e5f-1a2b3c4d5e6f' },
      schema: clienteSchema,
    })

    const chamada = fetchFalso.chamadas[0]
    expect(chamada?.metodo).toBe('POST')
    expect(chamada?.corpo).toEqual({ clienteId: 2, itens: [{ produtoId: 10, quantidade: 1 }] })
    expect(chamada?.cabecalhos.get('Content-Type')).toMatch(/application\/json/)
    expect(chamada?.cabecalhos.get('Idempotency-Key')).toBe('3f1c2a4e-8b7d-4c6a-9e5f-1a2b3c4d5e6f')
  })

  it('turns a business rule problem into an ApiError with status, code, meta and traceId', async () => {
    const problema = umProblema(422, 'estoque_insuficiente', {
      detail: 'O produto Teclado Mecânico tem 2 unidades em estoque e foram pedidas 5.',
      traceId: '00-abc-00',
      meta: { produtoId: 12, produtoNome: 'Teclado Mecânico', solicitado: 5, disponivel: 2 },
    })
    instalarFetchFalso({ 'POST /api/pedidos': () => respostaProblema(422, problema) })

    const erro = await capturarErro(requisitar('/pedidos', { metodo: 'POST', corpo: {} }))

    expect(erro.status).toBe(422)
    expect(erro.code).toBe('estoque_insuficiente')
    expect(erro.title).toBe('Estoque insuficiente')
    expect(erro.detail).toBe(problema.detail)
    expect(erro.traceId).toBe('00-abc-00')
    expect(erro.meta).toEqual({ produtoId: 12, produtoNome: 'Teclado Mecânico', solicitado: 5, disponivel: 2 })
    expect(erro.errors).toEqual([])
  })

  it('keeps every field error of a validation problem', async () => {
    const errors = [
      { campo: 'clienteId', code: 'cliente_id_invalido', mensagem: 'Cliente inválido.' },
      { campo: 'itens[0].quantidade', code: 'quantidade_invalida', mensagem: 'Quantidade inválida.' },
      { campo: 'itens[1].produtoId', code: 'produto_repetido', mensagem: 'Produto repetido.' },
    ]
    instalarFetchFalso({ 'POST /api/pedidos': () => respostaProblema(400, umProblema(400, 'validacao_falhou', { errors })) })

    const erro = await capturarErro(requisitar('/pedidos', { metodo: 'POST', corpo: {} }))

    expect(erro.status).toBe(400)
    expect(erro.code).toBe('validacao_falhou')
    expect(erro.errors).toEqual(errors)
    expect(erro.meta).toEqual({})
  })

  it('reads the Retry-After header of a 429 into retryAfterSegundos', async () => {
    const problema = umProblema(429, 'limite_requisicoes_excedido', { meta: { retryAfterSegundos: 30 } })
    instalarFetchFalso({ 'POST /api/pedidos': () => respostaProblema(429, problema, { 'Retry-After': '30' }) })

    const erro = await capturarErro(requisitar('/pedidos', { metodo: 'POST', corpo: {} }))

    expect(erro.status).toBe(429)
    expect(erro.code).toBe('limite_requisicoes_excedido')
    expect(erro.retryAfterSegundos).toBe(30)
  })

  it.each([
    [404, 'pedido_nao_encontrado'],
    [415, 'formato_nao_suportado'],
    [500, 'erro_interno'],
    [503, 'banco_indisponivel'],
  ])('turns a %i problem into an ApiError with code %s', async (status, code) => {
    instalarFetchFalso({ 'GET /api/pedidos/9': () => respostaProblema(status, umProblema(status, code, { traceId: 'trace-x' })) })

    const erro = await capturarErro(requisitar('/pedidos/9'))

    expect(erro.status).toBe(status)
    expect(erro.code).toBe(code)
    expect(erro.traceId).toBe('trace-x')
  })

  it('turns a network failure into sem_conexao', async () => {
    instalarFetchFalso({ 'GET /api/clientes': () => falhaDeRede() })

    const erro = await capturarErro(requisitar('/clientes'))

    expect(erro.code).toBe('sem_conexao')
  })

  it('turns an error response that is not a problem document into resposta_invalida', async () => {
    instalarFetchFalso({
      'GET /api/clientes': () => new Response('<html>Bad Gateway</html>', { status: 502, headers: { 'Content-Type': 'text/html' } }),
    })

    const erro = await capturarErro(requisitar('/clientes'))

    expect(erro.code).toBe('resposta_invalida')
  })

  it('turns an error response with JSON outside the problem format into resposta_invalida', async () => {
    instalarFetchFalso({ 'GET /api/clientes': () => respostaJson(500, { message: 'boom' }) })

    const erro = await capturarErro(requisitar('/clientes'))

    expect(erro.code).toBe('resposta_invalida')
  })

  it('turns a success body that does not match the schema into resposta_invalida', async () => {
    instalarFetchFalso({ 'GET /api/clientes': () => respostaJson(200, [{ id: 'um', nome: 7 }]) })

    const erro = await capturarErro(requisitar('/clientes', { schema: z.array(clienteSchema) }))

    expect(erro.code).toBe('resposta_invalida')
  })

  it('turns a success body that is not JSON into resposta_invalida', async () => {
    instalarFetchFalso({
      'GET /api/clientes': () => new Response('<html>index</html>', { status: 200, headers: { 'Content-Type': 'text/html' } }),
    })

    const erro = await capturarErro(requisitar('/clientes', { schema: z.array(clienteSchema) }))

    expect(erro.code).toBe('resposta_invalida')
  })
})
