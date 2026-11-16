import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import type { UserEvent } from '@testing-library/user-event'
import { NovoPedidoDialog } from '@/features/pedidos/components/NovoPedidoDialog'
import { PedidosPage } from '@/features/pedidos/pages/PedidosPage'
import { ApiError } from '@/shared/api/ApiError'
import { mensagemDoCampo, mensagemDoErro } from '@/shared/api/error-messages'
import type { ErroDeCampo, Produto } from '@/shared/api/types'
import { formatoUuid, umaPagina, umCliente, umPedidoDetalhe, umPedidoResumo, umProblema, umProduto } from '@/test/builders'
import {
  falhaDeRede,
  instalarFetchFalso,
  pendente,
  respostaJson,
  respostaProblema,
  type FetchFalso,
  type RequisicaoCapturada,
  type Rota,
} from '@/test/http'
import { renderizarComProvedores } from '@/test/render'

const clientes = [
  umCliente({ id: 1, nome: 'Ana Silva', email: 'ana@exemplo.com' }),
  umCliente({ id: 2, nome: 'Bruno Souza', email: 'bruno@exemplo.com' }),
]

const produtos: Produto[] = [
  umProduto({ id: 10, nome: 'Teclado Mecânico', sku: 'SKU-0010', preco: 150.5, estoque: 8 }),
  umProduto({ id: 11, nome: 'Mouse Óptico', sku: 'SKU-0011', preco: 49.9, estoque: 30 }),
  umProduto({ id: 12, nome: 'Monitor 24', sku: 'SKU-0012', preco: 899.9, estoque: 3 }),
]

function instalar(criar: Rota = () => respostaJson(201, umPedidoDetalhe()), extras: Record<string, Rota> = {}): FetchFalso {
  return instalarFetchFalso({
    'GET /api/clientes': () => respostaJson(200, clientes),
    'GET /api/produtos': () => respostaJson(200, produtos),
    'GET /api/pedidos': () => respostaJson(200, umaPagina([])),
    'POST /api/pedidos': criar,
    ...extras,
  })
}

function renderizarDialogo() {
  const onOpenChange = vi.fn<(aberto: boolean) => void>()
  const resultado = renderizarComProvedores(<NovoPedidoDialog open onOpenChange={onOpenChange} />)
  return { ...resultado, onOpenChange }
}

function dialogo(): HTMLElement {
  return screen.getByRole('dialog', { name: 'Novo pedido' })
}

function campoCliente(): HTMLSelectElement {
  return within(dialogo()).getByRole('combobox', { name: /^Cliente$/ }) as HTMLSelectElement
}

function selectsDeProduto(): HTMLSelectElement[] {
  return within(dialogo()).queryAllByRole('combobox', { name: /^Produto/ }) as HTMLSelectElement[]
}

function camposDeQuantidade(): HTMLInputElement[] {
  return within(dialogo()).queryAllByRole('spinbutton', { name: /^Quantidade/ }) as HTMLInputElement[]
}

function botaoCriar(): HTMLElement {
  return within(dialogo()).getByRole('button', { name: 'Criar pedido' })
}

function elemento<T>(lista: T[], indice: number): T {
  const item = lista[indice]
  if (item === undefined) throw new Error(`elemento ${indice} ausente`)
  return item
}

async function garantirLinhas(user: UserEvent, quantidade: number) {
  for (let tentativa = 0; selectsDeProduto().length < quantidade && tentativa < quantidade + 5; tentativa += 1) {
    await user.click(within(dialogo()).getByRole('button', { name: 'Adicionar item' }))
  }
  expect(selectsDeProduto().length).toBeGreaterThanOrEqual(quantidade)
}

async function escolherCliente(user: UserEvent, nome: string) {
  const select = campoCliente()
  const opcao = await within(select).findByRole('option', { name: nome })
  await user.selectOptions(select, opcao)
}

async function preencherItem(user: UserEvent, indice: number, produto: RegExp, quantidade: string) {
  await garantirLinhas(user, indice + 1)
  const select = elemento(selectsDeProduto(), indice)
  const opcao = await within(select).findByRole('option', { name: produto })
  await user.selectOptions(select, opcao)
  const campo = elemento(camposDeQuantidade(), indice)
  await user.clear(campo)
  if (quantidade !== '') await user.type(campo, quantidade)
}

async function preencherPedidoValido(user: UserEvent) {
  await escolherCliente(user, 'Bruno Souza')
  await preencherItem(user, 0, /Teclado Mecânico/, '2')
  await preencherItem(user, 1, /Monitor 24/, '1')
}

function envios(fetchFalso: FetchFalso): RequisicaoCapturada[] {
  return fetchFalso.chamadasDe('POST', '/api/pedidos')
}

function produtoIdsDe(requisicao: RequisicaoCapturada): number[] {
  const corpo = requisicao.corpo as { itens?: { produtoId: number }[] }
  return (corpo.itens ?? []).map((item) => item.produtoId)
}

function problemaDeRegra(code: string, meta: Record<string, unknown>) {
  return umProblema(422, code, { meta, detail: 'Detalhe técnico do servidor' })
}

function erroEsperado(status: number, code: string, meta: Record<string, unknown> = {}, retryAfterSegundos?: number): string {
  return mensagemDoErro(new ApiError({ status, code, title: 'x', detail: 'x', meta, retryAfterSegundos }))
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('NovoPedidoDialog', () => {
  it('shows the title, the clients and the products with their stock', async () => {
    instalar()
    const { user } = renderizarDialogo()

    expect(dialogo()).toBeInTheDocument()
    expect(await within(campoCliente()).findByRole('option', { name: 'Ana Silva' })).toBeInTheDocument()
    expect(within(campoCliente()).getByRole('option', { name: 'Bruno Souza' })).toBeInTheDocument()
    await garantirLinhas(user, 1)
    const select = elemento(selectsDeProduto(), 0)
    expect(await within(select).findByRole('option', { name: /Teclado Mecânico.*\b8\b/ })).toBeInTheDocument()
    expect(within(select).getByRole('option', { name: /Monitor 24.*\b3\b/ })).toBeInTheDocument()
    expect(within(select).getByRole('option', { name: /Mouse Óptico.*\b30\b/ })).toBeInTheDocument()
  })

  it('shows the subtotal of each item and the order total in BRL', async () => {
    instalar()
    const { user } = renderizarDialogo()

    await preencherPedidoValido(user)

    expect(dialogo()).toHaveTextContent('R$ 301,00')
    expect(dialogo()).toHaveTextContent('R$ 899,90')
    expect(dialogo()).toHaveTextContent(/Total/)
    expect(dialogo()).toHaveTextContent('R$ 1.200,90')
  })

  it('updates the subtotal and the total when a quantity changes', async () => {
    instalar()
    const { user } = renderizarDialogo()
    await preencherPedidoValido(user)

    const campo = elemento(camposDeQuantidade(), 0)
    await user.clear(campo)
    await user.type(campo, '3')

    expect(dialogo()).toHaveTextContent('R$ 451,50')
    expect(dialogo()).toHaveTextContent('R$ 1.351,40')
  })

  it('removes an item and updates the total', async () => {
    instalar()
    const { user } = renderizarDialogo()
    await preencherPedidoValido(user)

    const botoesRemover = within(dialogo()).getAllByRole('button', { name: /^Remover item/ })
    await user.click(elemento(botoesRemover, 1))

    expect(selectsDeProduto()).toHaveLength(1)
    expect(dialogo()).not.toHaveTextContent('R$ 899,90')
    expect(dialogo()).not.toHaveTextContent('R$ 1.200,90')
    expect(dialogo()).toHaveTextContent('R$ 301,00')
  })

  it('sends the client and the items without price and with an idempotency key, then closes', async () => {
    const fetchFalso = instalar()
    const { user, onOpenChange } = renderizarDialogo()
    await preencherPedidoValido(user)

    await user.click(botaoCriar())

    await waitFor(() => expect(onOpenChange).toHaveBeenCalledWith(false))
    const enviados = envios(fetchFalso)
    expect(enviados).toHaveLength(1)
    const envio = elemento(enviados, 0)
    expect(envio.corpo).toEqual({
      clienteId: 2,
      itens: [
        { produtoId: 10, quantidade: 2 },
        { produtoId: 12, quantidade: 1 },
      ],
    })
    expect(envio.cabecalhos.get('Idempotency-Key')).toMatch(formatoUuid)
    expect(envio.cabecalhos.get('Content-Type')).toMatch(/application\/json/)
  })

  it('requires a client, shows the message next to it and focuses it', async () => {
    const fetchFalso = instalar()
    const { user } = renderizarDialogo()
    await preencherItem(user, 0, /Teclado Mecânico/, '1')

    await user.click(botaoCriar())

    await waitFor(() => expect(campoCliente()).toHaveAccessibleDescription(/.+/))
    expect(campoCliente()).toHaveFocus()
    expect(within(dialogo()).getAllByRole('alert').length).toBeGreaterThan(0)
    expect(envios(fetchFalso)).toHaveLength(0)
  })

  it('requires a product in each item', async () => {
    const fetchFalso = instalar()
    const { user } = renderizarDialogo()
    await escolherCliente(user, 'Bruno Souza')
    await garantirLinhas(user, 1)
    const quantidade = elemento(camposDeQuantidade(), 0)
    await user.clear(quantidade)
    await user.type(quantidade, '1')

    await user.click(botaoCriar())

    await waitFor(() => expect(elemento(selectsDeProduto(), 0)).toHaveAccessibleDescription(/.+/))
    expect(envios(fetchFalso)).toHaveLength(0)
  })

  it.each(['0', '-3', '1.5', ''])('rejects the quantity "%s" next to the field without sending', async (valor) => {
    const fetchFalso = instalar()
    const { user } = renderizarDialogo()
    await escolherCliente(user, 'Bruno Souza')
    await preencherItem(user, 0, /Teclado Mecânico/, valor)

    await user.click(botaoCriar())

    const campo = elemento(camposDeQuantidade(), 0)
    await waitFor(() => expect(campo).toHaveAccessibleDescription(/.+/))
    expect(campo).toHaveFocus()
    expect(envios(fetchFalso)).toHaveLength(0)
  })

  it('never sends an order without items', async () => {
    const fetchFalso = instalar()
    const { user } = renderizarDialogo()
    await escolherCliente(user, 'Bruno Souza')
    await garantirLinhas(user, 1)

    for (let tentativa = 0; tentativa < 5; tentativa += 1) {
      const remover = within(dialogo()).queryAllByRole('button', { name: /^Remover item/ }).find((botao) => !botao.hasAttribute('disabled'))
      if (!remover) break
      await user.click(remover)
    }
    await user.click(botaoCriar())

    await waitFor(() => expect(within(dialogo()).getAllByRole('alert').length).toBeGreaterThan(0))
    expect(envios(fetchFalso)).toHaveLength(0)
  })

  it('never sends more than 50 items', async () => {
    const muitosProdutos = Array.from({ length: 55 }, (_, indice) =>
      umProduto({ id: 100 + indice, nome: `Item ${indice + 1}`, sku: `SKU-${100 + indice}`, preco: 1, estoque: 100 }),
    )
    const fetchFalso = instalar(undefined, { 'GET /api/produtos': () => respostaJson(200, muitosProdutos) })
    const { user } = renderizarDialogo()
    await escolherCliente(user, 'Bruno Souza')
    await garantirLinhas(user, 1)
    await within(elemento(selectsDeProduto(), 0)).findByRole('option', { name: /Item 55/ })

    for (let tentativa = 0; tentativa < 55 && selectsDeProduto().length < 51; tentativa += 1) {
      const adicionar = within(dialogo()).getByRole('button', { name: 'Adicionar item' })
      if (adicionar.hasAttribute('disabled')) break
      fireEvent.click(adicionar)
    }
    const selects = selectsDeProduto()
    selects.forEach((select, indice) => {
      const nome = new RegExp(`Item ${indice + 1}(?!\\d)`)
      const opcao = Array.from(select.options).find((candidata) => nome.test(candidata.text))
      if (!opcao) throw new Error(`opção Item ${indice + 1} ausente`)
      fireEvent.change(select, { target: { value: opcao.value } })
    })
    camposDeQuantidade().forEach((campo) => fireEvent.change(campo, { target: { value: '1' } }))
    await user.click(botaoCriar())

    await waitFor(() => expect(envios(fetchFalso).length > 0 || within(dialogo()).queryAllByRole('alert').length > 0).toBe(true))
    expect(envios(fetchFalso).every((envio) => produtoIdsDe(envio).length <= 50)).toBe(true)
    expect(selectsDeProduto().length <= 50 || envios(fetchFalso).length === 0).toBe(true)
  }, 30000)

  it('does not let the same product be chosen in two items', async () => {
    const fetchFalso = instalar()
    const { user } = renderizarDialogo()
    await escolherCliente(user, 'Bruno Souza')
    await preencherItem(user, 0, /Teclado Mecânico/, '1')
    await garantirLinhas(user, 2)

    const primeiro = elemento(selectsDeProduto(), 0)
    const segundo = elemento(selectsDeProduto(), 1)
    const repetida = within(segundo).queryByRole('option', { name: /Teclado Mecânico/ }) as HTMLOptionElement | null
    expect(repetida === null || repetida.disabled).toBe(true)

    fireEvent.change(segundo, { target: { value: primeiro.value } })
    const quantidade = elemento(camposDeQuantidade(), 1)
    await user.clear(quantidade)
    await user.type(quantidade, '1')
    await user.click(botaoCriar())

    await waitFor(() => expect(envios(fetchFalso).length > 0 || within(dialogo()).queryAllByRole('alert').length > 0).toBe(true))
    for (const envio of envios(fetchFalso)) {
      const ids = produtoIdsDe(envio)
      expect(new Set(ids).size).toBe(ids.length)
    }
  })

  it('disables the submit button while sending and sends only once', async () => {
    const espera = pendente()
    const fetchFalso = instalar(() => espera.promessa)
    const { user, onOpenChange } = renderizarDialogo()
    await preencherPedidoValido(user)

    await user.click(botaoCriar())

    await waitFor(() => expect(botaoCriar()).toBeDisabled())
    await user.click(botaoCriar())
    expect(envios(fetchFalso)).toHaveLength(1)

    espera.resolver(respostaJson(201, umPedidoDetalhe()))
    await waitFor(() => expect(onOpenChange).toHaveBeenCalledWith(false))
    expect(envios(fetchFalso)).toHaveLength(1)
  })

  it('uses a new idempotency key on every submit attempt', async () => {
    let tentativas = 0
    const fetchFalso = instalar(() => {
      tentativas += 1
      return tentativas === 1
        ? respostaProblema(422, problemaDeRegra('estoque_insuficiente', { produtoId: 10, produtoNome: 'Teclado Mecânico', solicitado: 9, disponivel: 8 }))
        : respostaJson(201, umPedidoDetalhe())
    })
    const { user, onOpenChange } = renderizarDialogo()
    await escolherCliente(user, 'Bruno Souza')
    await preencherItem(user, 0, /Teclado Mecânico/, '9')
    await user.click(botaoCriar())
    await within(dialogo()).findByRole('alert')

    const campo = elemento(camposDeQuantidade(), 0)
    await user.clear(campo)
    await user.type(campo, '8')
    await user.click(botaoCriar())

    await waitFor(() => expect(onOpenChange).toHaveBeenCalledWith(false))
    const [primeiro, segundo] = envios(fetchFalso)
    expect(primeiro?.cabecalhos.get('Idempotency-Key')).toMatch(formatoUuid)
    expect(segundo?.cabecalhos.get('Idempotency-Key')).toMatch(formatoUuid)
    expect(segundo?.cabecalhos.get('Idempotency-Key')).not.toBe(primeiro?.cabecalhos.get('Idempotency-Key'))
  })

  it('retries from the alert with the same order and a new idempotency key', async () => {
    let tentativas = 0
    const fetchFalso = instalar(() => {
      tentativas += 1
      return tentativas === 1
        ? respostaProblema(503, umProblema(503, 'banco_indisponivel', { traceId: 'trace-503' }))
        : respostaJson(201, umPedidoDetalhe())
    })
    const { user, onOpenChange } = renderizarDialogo()
    await preencherPedidoValido(user)
    await user.click(botaoCriar())

    const alerta = await within(dialogo()).findByRole('alert')
    await user.click(within(alerta).getByRole('button', { name: 'Tentar de novo' }))

    await waitFor(() => expect(onOpenChange).toHaveBeenCalledWith(false))
    const [primeiro, segundo] = envios(fetchFalso)
    expect(segundo?.corpo).toEqual(primeiro?.corpo)
    expect(segundo?.cabecalhos.get('Idempotency-Key')).toMatch(formatoUuid)
    expect(segundo?.cabecalhos.get('Idempotency-Key')).not.toBe(primeiro?.cabecalhos.get('Idempotency-Key'))
  })

  it('keeps the dialog open with the data and names the product and the available stock when stock is insufficient', async () => {
    const meta = { produtoId: 10, produtoNome: 'Teclado Mecânico', solicitado: 9, disponivel: 8 }
    const fetchFalso = instalar(() => respostaProblema(422, problemaDeRegra('estoque_insuficiente', meta)))
    const { user, onOpenChange } = renderizarDialogo()
    await escolherCliente(user, 'Bruno Souza')
    await preencherItem(user, 0, /Teclado Mecânico/, '9')

    await user.click(botaoCriar())

    const alerta = await within(dialogo()).findByRole('alert')
    expect(envios(fetchFalso)).toHaveLength(1)
    expect(alerta).toHaveTextContent(erroEsperado(422, 'estoque_insuficiente', meta))
    expect(alerta).toHaveTextContent('Teclado Mecânico')
    expect(alerta).toHaveTextContent(/\b8\b/)
    expect(alerta).not.toHaveTextContent('Detalhe técnico do servidor')
    expect(onOpenChange).not.toHaveBeenCalledWith(false)
    expect(campoCliente()).toHaveDisplayValue('Bruno Souza')
    expect(elemento(selectsDeProduto(), 0)).toHaveDisplayValue(/Teclado Mecânico/)
    expect(elemento(camposDeQuantidade(), 0)).toHaveValue(9)
    expect(botaoCriar()).toBeEnabled()
  })

  it.each([
    ['cliente_inexistente', { clienteId: 2 }],
    ['produto_inexistente', { produtoId: 12 }],
  ])('keeps the dialog open and shows the specific message for %s', async (code, meta) => {
    instalar(() => respostaProblema(422, problemaDeRegra(code, meta)))
    const { user, onOpenChange } = renderizarDialogo()
    await preencherPedidoValido(user)

    await user.click(botaoCriar())

    const alerta = await within(dialogo()).findByRole('alert')
    expect(alerta).toHaveTextContent(erroEsperado(422, code, meta))
    expect(onOpenChange).not.toHaveBeenCalledWith(false)
    expect(campoCliente()).toHaveDisplayValue('Bruno Souza')
    expect(selectsDeProduto()).toHaveLength(2)
  })

  it('puts each field error returned by the API next to the right field', async () => {
    const errosDeCampo: ErroDeCampo[] = [
      { campo: 'clienteId', code: 'cliente_id_invalido', mensagem: 'Cliente inválido.' },
      { campo: 'itens[1].quantidade', code: 'quantidade_invalida', mensagem: 'Quantidade inválida.' },
    ]
    instalar(() => respostaProblema(400, umProblema(400, 'validacao_falhou', { errors: errosDeCampo })))
    const { user, onOpenChange } = renderizarDialogo()
    await preencherPedidoValido(user)

    await user.click(botaoCriar())

    const [erroDoCliente, erroDaQuantidade] = errosDeCampo
    if (!erroDoCliente || !erroDaQuantidade) throw new Error('erros ausentes')
    await waitFor(() => expect(campoCliente()).toHaveAccessibleDescription(expect.stringContaining(mensagemDoCampo(erroDoCliente))))
    expect(elemento(camposDeQuantidade(), 1)).toHaveAccessibleDescription(expect.stringContaining(mensagemDoCampo(erroDaQuantidade)))
    expect(elemento(camposDeQuantidade(), 0)).not.toHaveAccessibleDescription(expect.stringContaining(mensagemDoCampo(erroDaQuantidade)))
    expect(onOpenChange).not.toHaveBeenCalledWith(false)
  })

  it('shows a body error returned by the API in the form alert', async () => {
    const erroDoCorpo: ErroDeCampo = { campo: '$', code: 'corpo_invalido', mensagem: 'JSON inválido.' }
    instalar(() => respostaProblema(400, umProblema(400, 'validacao_falhou', { errors: [erroDoCorpo] })))
    const { user, onOpenChange } = renderizarDialogo()
    await preencherPedidoValido(user)

    await user.click(botaoCriar())

    const mensagens = await within(dialogo()).findAllByText(mensagemDoCampo(erroDoCorpo), { exact: false })
    expect(mensagens.length).toBeGreaterThan(0)
    expect(onOpenChange).not.toHaveBeenCalledWith(false)
  })

  it('shows the wait and a retry action when the rate limit is exceeded', async () => {
    instalar(() =>
      respostaProblema(429, umProblema(429, 'limite_requisicoes_excedido', { meta: { retryAfterSegundos: 30 } }), { 'Retry-After': '30' }),
    )
    const { user, onOpenChange } = renderizarDialogo()
    await preencherPedidoValido(user)

    await user.click(botaoCriar())

    const alerta = await within(dialogo()).findByRole('alert')
    expect(alerta).toHaveTextContent(erroEsperado(429, 'limite_requisicoes_excedido', { retryAfterSegundos: 30 }, 30))
    expect(alerta).toHaveTextContent(/30\s*segundo/i)
    expect(within(alerta).getByRole('button', { name: 'Tentar de novo' })).toBeInTheDocument()
    expect(onOpenChange).not.toHaveBeenCalledWith(false)
  })

  it.each([
    [500, 'erro_interno'],
    [503, 'banco_indisponivel'],
  ])('shows the support code and a retry action on %i %s', async (status, code) => {
    instalar(() => respostaProblema(status, umProblema(status, code, { traceId: 'trace-suporte-9', detail: 'SqlException at dbo.Pedidos' })))
    const { user, onOpenChange } = renderizarDialogo()
    await preencherPedidoValido(user)

    await user.click(botaoCriar())

    const alerta = await within(dialogo()).findByRole('alert')
    expect(alerta).toHaveTextContent(erroEsperado(status, code))
    expect(alerta).toHaveTextContent('Código de suporte: trace-suporte-9')
    expect(alerta).not.toHaveTextContent(/SqlException|dbo\./)
    expect(within(alerta).getByRole('button', { name: 'Tentar de novo' })).toBeInTheDocument()
    expect(onOpenChange).not.toHaveBeenCalledWith(false)
    expect(campoCliente()).toHaveDisplayValue('Bruno Souza')
  })

  it('shows the no connection message and a retry action when the network fails', async () => {
    instalar(() => falhaDeRede())
    const { user, onOpenChange } = renderizarDialogo()
    await preencherPedidoValido(user)

    await user.click(botaoCriar())

    const alerta = await within(dialogo()).findByRole('alert')
    expect(alerta).toHaveTextContent(erroEsperado(0, 'sem_conexao'))
    expect(within(alerta).getByRole('button', { name: 'Tentar de novo' })).toBeInTheDocument()
    expect(onOpenChange).not.toHaveBeenCalledWith(false)
  })

  it('shows an error when the products cannot be loaded', async () => {
    instalar(undefined, {
      'GET /api/produtos': () => respostaProblema(503, umProblema(503, 'banco_indisponivel', { traceId: 'trace-produtos' })),
    })

    renderizarDialogo()

    const alerta = await within(dialogo()).findByRole('alert')
    expect(alerta).toHaveTextContent(erroEsperado(503, 'banco_indisponivel'))
  })

  it('closes on cancel without sending anything', async () => {
    const fetchFalso = instalar()
    const { user, onOpenChange } = renderizarDialogo()
    await escolherCliente(user, 'Bruno Souza')

    await user.click(within(dialogo()).getByRole('button', { name: 'Cancelar' }))

    expect(onOpenChange).toHaveBeenCalledWith(false)
    expect(envios(fetchFalso)).toHaveLength(0)
  })

  it('refreshes the orders list after creating an order', async () => {
    const novo = umPedidoResumo({ id: 5001, clienteId: 2, clienteNome: 'Bruno Souza', dataPedido: '2026-11-13T12:00:00Z', valorTotal: 1200.9, quantidadeItens: 2 })
    let criado = false
    const fetchFalso = instalar(
      () => {
        criado = true
        return respostaJson(201, umPedidoDetalhe())
      },
      { 'GET /api/pedidos': () => respostaJson(200, umaPagina(criado ? [novo] : [])) },
    )
    const { user } = renderizarComProvedores(<PedidosPage />)
    await screen.findByText('Nenhum pedido encontrado')

    await user.click(screen.getByRole('button', { name: 'Novo pedido' }))
    await within(await screen.findByRole('dialog', { name: 'Novo pedido' })).findByRole('combobox', { name: /^Cliente$/ })
    await preencherPedidoValido(user)
    await user.click(botaoCriar())

    await waitFor(() => expect(screen.queryByRole('dialog', { name: 'Novo pedido' })).not.toBeInTheDocument())
    const linha = await screen.findByRole('row', { name: /5001/ })
    expect(linha).toHaveTextContent('R$ 1.200,90')
    expect(fetchFalso.chamadasDe('GET', '/api/pedidos').length).toBeGreaterThanOrEqual(2)
  })
})
