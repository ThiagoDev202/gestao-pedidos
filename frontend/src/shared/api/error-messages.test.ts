import { ApiError } from '@/shared/api/ApiError'
import { mensagemDoCampo, mensagemDoErro } from '@/shared/api/error-messages'

interface CasoDeErro {
  status: number
  code: string
  meta?: Record<string, unknown>
  retryAfterSegundos?: number
  esperado: RegExp[]
}

const detalheTecnico = "SqlException (0x80131904): Invalid object name 'dbo.Pedidos'. at GestaoPedidos.Infrastructure.PedidoRepository.Salvar()"

const casos: CasoDeErro[] = [
  { status: 400, code: 'validacao_falhou', esperado: [/dados|campos?|inválid|corrij/i] },
  { status: 404, code: 'pedido_nao_encontrado', meta: { pedidoId: 77 }, esperado: [/pedido/i, /encontrad|existe/i] },
  { status: 404, code: 'recurso_nao_encontrado', esperado: [/encontrad|existe/i] },
  { status: 415, code: 'formato_nao_suportado', esperado: [/formato|suportad/i] },
  { status: 422, code: 'cliente_inexistente', meta: { clienteId: 99 }, esperado: [/cliente/i] },
  { status: 422, code: 'produto_inexistente', meta: { produtoId: 12 }, esperado: [/produto/i] },
  {
    status: 422,
    code: 'estoque_insuficiente',
    meta: { produtoId: 12, produtoNome: 'Teclado Mecânico', solicitado: 5, disponivel: 2 },
    esperado: [/Teclado Mecânico/, /\b5\b/, /\b2\b/],
  },
  { status: 422, code: 'chave_reutilizada', meta: { chave: '3f1c2a4e-8b7d-4c6a-9e5f-1a2b3c4d5e6f' }, esperado: [/pedido|envi|chave/i] },
  {
    status: 429,
    code: 'limite_requisicoes_excedido',
    meta: { retryAfterSegundos: 30 },
    retryAfterSegundos: 30,
    esperado: [/30\s*segundo/i],
  },
  { status: 500, code: 'erro_interno', esperado: [/inesperad|erro|problema/i] },
  { status: 503, code: 'banco_indisponivel', esperado: [/indispon|instantes|tente/i] },
  { status: 0, code: 'sem_conexao', esperado: [/conex|internet|rede|servidor/i] },
  { status: 502, code: 'resposta_invalida', esperado: [/resposta|inesperad|inválid/i] },
]

const codigosDeCampo = [
  'corpo_invalido',
  'cliente_id_invalido',
  'itens_vazio',
  'itens_excedem_limite',
  'produto_id_invalido',
  'quantidade_invalida',
  'produto_repetido',
  'chave_idempotencia_invalida',
  'pagina_invalida',
  'tamanho_pagina_invalido',
  'data_invalida',
  'periodo_obrigatorio',
  'periodo_invertido',
]

const textoTecnico = [/undefined/i, /\bnull\b/i, /NaN/, /\[object/i, /SqlException/i, /dbo\./i, /\bat\s+\w+\./, /Exception/, /[{}]/]

function erroDe(caso: Pick<CasoDeErro, 'status' | 'code' | 'meta' | 'retryAfterSegundos'>, detail = detalheTecnico): ApiError {
  return new ApiError({
    status: caso.status,
    code: caso.code,
    title: 'Título técnico',
    detail,
    meta: caso.meta,
    retryAfterSegundos: caso.retryAfterSegundos,
    traceId: '00-4bf92f3577b34da6a3ce929d0e0e4736-00',
  })
}

function semTextoTecnico(mensagem: string) {
  for (const padrao of textoTecnico) expect(mensagem).not.toMatch(padrao)
}

describe('mensagemDoErro', () => {
  it.each(casos)('gives a specific pt-BR message for $code', (caso) => {
    const mensagem = mensagemDoErro(erroDe(caso))

    expect(mensagem.trim().length).toBeGreaterThan(10)
    for (const padrao of caso.esperado) expect(mensagem).toMatch(padrao)
    expect(mensagem).not.toContain(caso.code)
    expect(mensagem).not.toContain(detalheTecnico)
    semTextoTecnico(mensagem)
  })

  it('gives a distinct message to every code of the catalog', () => {
    const mensagens = casos.map((caso) => mensagemDoErro(erroDe(caso)))

    expect(new Set(mensagens).size).toBe(casos.length)
  })

  it('names the product, the requested and the available quantities when stock is insufficient', () => {
    const mensagem = mensagemDoErro(
      erroDe({ status: 422, code: 'estoque_insuficiente', meta: { produtoId: 3, produtoNome: 'Monitor 24', solicitado: 9, disponivel: 4 } }),
    )

    expect(mensagem).toContain('Monitor 24')
    expect(mensagem).toMatch(/\b9\b/)
    expect(mensagem).toMatch(/\b4\b/)
  })

  it('cites the wait from the Retry-After header when the 429 has no meta', () => {
    const mensagem = mensagemDoErro(erroDe({ status: 429, code: 'limite_requisicoes_excedido', retryAfterSegundos: 45 }))

    expect(mensagem).toMatch(/45\s*segundo/i)
    semTextoTecnico(mensagem)
  })

  it.each(casos)('never shows technical text for $code when meta is missing', (caso) => {
    const mensagem = mensagemDoErro(erroDe({ status: caso.status, code: caso.code }))

    expect(mensagem.trim().length).toBeGreaterThan(10)
    semTextoTecnico(mensagem)
  })

  it.each([400, 401, 403, 404, 409, 422, 429, 500, 502, 503])(
    'falls back to a generic message for status %i when the code is unknown',
    (status) => {
      const mensagem = mensagemDoErro(erroDe({ status, code: 'codigo_que_nao_existe' }))

      expect(mensagem.trim().length).toBeGreaterThan(10)
      expect(mensagem).not.toContain('codigo_que_nao_existe')
      expect(mensagem).not.toContain('Título técnico')
      expect(mensagem).not.toContain(detalheTecnico)
      semTextoTecnico(mensagem)
    },
  )

  it('uses different generic messages for client and server failures with unknown codes', () => {
    const naoEncontrado = mensagemDoErro(erroDe({ status: 404, code: 'codigo_que_nao_existe' }))
    const falhaDoServidor = mensagemDoErro(erroDe({ status: 500, code: 'codigo_que_nao_existe' }))

    expect(naoEncontrado).not.toBe(falhaDoServidor)
  })

  it('never shows the detail of a server failure', () => {
    const mensagem = mensagemDoErro(erroDe({ status: 500, code: 'erro_interno' }, 'Timeout expired. The timeout period elapsed'))

    expect(mensagem).not.toContain('Timeout expired')
  })
})

describe('mensagemDoCampo', () => {
  it.each(codigosDeCampo)('gives a specific pt-BR message for the field code %s', (code) => {
    const mensagem = mensagemDoCampo({ campo: 'itens[0].quantidade', code, mensagem: detalheTecnico })

    expect(mensagem.trim().length).toBeGreaterThan(5)
    expect(mensagem).not.toContain(code)
    expect(mensagem).not.toContain(detalheTecnico)
    semTextoTecnico(mensagem)
  })

  it('gives a distinct message to every field code', () => {
    const mensagens = codigosDeCampo.map((code) => mensagemDoCampo({ campo: 'x', code, mensagem: 'x' }))

    expect(new Set(mensagens).size).toBe(codigosDeCampo.length)
  })

  it.each([
    ['quantidade_invalida', /quantidade/i],
    ['produto_repetido', /produto/i],
    ['itens_vazio', /item|itens/i],
    ['itens_excedem_limite', /50/],
    ['cliente_id_invalido', /cliente/i],
    ['periodo_invertido', /data|período/i],
  ])('explains the field problem %s in plain words', (code, esperado) => {
    expect(mensagemDoCampo({ campo: 'x', code, mensagem: 'x' })).toMatch(esperado)
  })

  it('never shows the raw code of an unknown field problem', () => {
    const mensagem = mensagemDoCampo({ campo: 'itens[0].quantidade', code: 'codigo_de_campo_novo', mensagem: 'Valor inválido.' })

    expect(mensagem.trim().length).toBeGreaterThan(5)
    expect(mensagem).not.toContain('codigo_de_campo_novo')
    semTextoTecnico(mensagem)
  })
})
