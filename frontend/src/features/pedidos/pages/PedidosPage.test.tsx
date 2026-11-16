import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { PedidosPage } from '@/features/pedidos/pages/PedidosPage'
import { mensagemDoCampo } from '@/shared/api/error-messages'
import type { PedidoResumo } from '@/shared/api/types'
import { dataLocal, horaLocal, moeda, umaPagina, umCliente, umPedidoResumo, umProblema, umProduto } from '@/test/builders'
import { falhaDeRede, instalarFetchFalso, pendente, respostaJson, respostaProblema, type FetchFalso, type Rota } from '@/test/http'
import { parametrosDaUrl, renderizarComProvedores } from '@/test/render'

const clientes = [
  umCliente({ id: 1, nome: 'Ana Silva', email: 'ana@exemplo.com' }),
  umCliente({ id: 7, nome: 'Bruno Souza', email: 'bruno@exemplo.com' }),
]

const pedidos: PedidoResumo[] = [
  umPedidoResumo({ id: 1002, clienteId: 7, clienteNome: 'Bruno Souza', dataPedido: '2026-03-15T23:30:00Z', valorTotal: 1234.5, quantidadeItens: 3 }),
  umPedidoResumo({ id: 1001, clienteId: 1, clienteNome: 'Ana Silva', dataPedido: '2026-03-14T09:05:00Z', valorTotal: 49.9, quantidadeItens: 1 }),
]

function instalar(listagem: Rota = () => respostaJson(200, umaPagina(pedidos))): FetchFalso {
  return instalarFetchFalso({
    'GET /api/pedidos': listagem,
    'GET /api/clientes': () => respostaJson(200, clientes),
    'GET /api/produtos': () => respostaJson(200, [umProduto()]),
  })
}

function paginada(totalItems: number): Rota {
  return (requisicao) => {
    const page = Number(requisicao.url.searchParams.get('page') ?? '1')
    return respostaJson(200, umaPagina(pedidos, { page, pageSize: 20, totalItems, totalPages: Math.ceil(totalItems / 20) }))
  }
}

function ultimaListagem(fetchFalso: FetchFalso): URLSearchParams {
  const chamadas = fetchFalso.chamadasDe('GET', '/api/pedidos')
  const ultima = chamadas[chamadas.length - 1]
  if (!ultima) throw new Error('a listagem não foi chamada')
  return ultima.url.searchParams
}

function linhasDeDados(): HTMLElement[] {
  return within(screen.getByRole('table')).getAllByRole('row').slice(1)
}

async function aguardarClientes() {
  const filtro = screen.getByRole('combobox', { name: 'Cliente' })
  await within(filtro).findByRole('option', { name: 'Bruno Souza' })
  return filtro
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('PedidosPage', () => {
  it('shows the title and the orders table with its columns', async () => {
    instalar()

    renderizarComProvedores(<PedidosPage />)

    expect(screen.getByRole('heading', { level: 1, name: 'Pedidos' })).toBeInTheDocument()
    await screen.findByText('Bruno Souza', { selector: 'td, td *' })
    for (const coluna of ['Pedido', 'Cliente', 'Data', 'Itens', 'Total']) {
      expect(screen.getByRole('columnheader', { name: coluna })).toBeInTheDocument()
    }
  })

  it('shows each order with id, client, local date, item count and total in BRL', async () => {
    instalar()

    renderizarComProvedores(<PedidosPage />)

    await screen.findByText('Bruno Souza', { selector: 'td, td *' })
    const [primeira] = linhasDeDados()
    if (!primeira) throw new Error('linha ausente')
    const celulas = within(primeira).getAllByRole('cell')
    expect(celulas[0]).toHaveTextContent('1002')
    expect(celulas[1]).toHaveTextContent('Bruno Souza')
    expect(celulas[2]).toHaveTextContent(dataLocal('2026-03-15T23:30:00Z'))
    expect(celulas[2]).toHaveTextContent(horaLocal('2026-03-15T23:30:00Z'))
    expect(celulas[3]).toHaveTextContent(/\b3\b/)
    expect(celulas[4]).toHaveTextContent('R$ 1.234,50')
    expect(celulas[4]).toHaveTextContent(moeda(1234.5))
  })

  it('keeps the order returned by the API, most recent first', async () => {
    instalar()

    renderizarComProvedores(<PedidosPage />)

    await screen.findByText('Bruno Souza', { selector: 'td, td *' })
    const linhas = linhasDeDados()
    expect(linhas).toHaveLength(2)
    expect(linhas[0]).toHaveTextContent('1002')
    expect(linhas[1]).toHaveTextContent('1001')
    expect(linhas[1]).toHaveTextContent(dataLocal('2026-03-14T09:05:00Z'))
    expect(linhas[1]).toHaveTextContent(horaLocal('2026-03-14T09:05:00Z'))
  })

  it('requests the first page with the default page size and no filters', async () => {
    const fetchFalso = instalar()

    renderizarComProvedores(<PedidosPage />)

    await waitFor(() => expect(fetchFalso.chamadasDe('GET', '/api/pedidos')).toHaveLength(1))
    const parametros = ultimaListagem(fetchFalso)
    expect(parametros.get('page')).toBe('1')
    expect(parametros.get('pageSize')).toBe('20')
    expect(parametros.has('clienteId')).toBe(false)
    expect(parametros.has('dataInicio')).toBe(false)
    expect(parametros.has('dataFim')).toBe(false)
  })

  it('reads filters and page from the URL and sends them to the API', async () => {
    const fetchFalso = instalar(paginada(60))

    renderizarComProvedores(<PedidosPage />, { rota: '/?clienteId=7&dataInicio=2026-01-01&dataFim=2026-01-31&page=2' })

    await waitFor(() => expect(fetchFalso.chamadasDe('GET', '/api/pedidos').length).toBeGreaterThan(0))
    const parametros = ultimaListagem(fetchFalso)
    expect(parametros.get('clienteId')).toBe('7')
    expect(parametros.get('dataInicio')).toBe('2026-01-01')
    expect(parametros.get('dataFim')).toBe('2026-01-31')
    expect(parametros.get('page')).toBe('2')
    const filtro = await aguardarClientes()
    await waitFor(() => expect(filtro).toHaveDisplayValue('Bruno Souza'))
    expect(screen.getByLabelText('Data inicial')).toHaveValue('2026-01-01')
    expect(screen.getByLabelText('Data final')).toHaveValue('2026-01-31')
  })

  it.each(['0', '-1', 'abc'])('requests the first page when the page in the URL is %s', async (pagina) => {
    const fetchFalso = instalar()

    renderizarComProvedores(<PedidosPage />, { rota: `/?page=${pagina}` })

    await waitFor(() => expect(fetchFalso.chamadasDe('GET', '/api/pedidos').length).toBeGreaterThan(0))
    expect(ultimaListagem(fetchFalso).get('page')).toBe('1')
  })

  it('filters by client, writes it to the URL and goes back to the first page', async () => {
    const fetchFalso = instalar(paginada(60))
    const { user } = renderizarComProvedores(<PedidosPage />, { rota: '/?page=3' })
    const filtro = await aguardarClientes()

    expect(within(filtro).getByRole('option', { name: 'Todos os clientes' })).toBeInTheDocument()
    await user.selectOptions(filtro, within(filtro).getByRole('option', { name: 'Bruno Souza' }))

    await waitFor(() => expect(ultimaListagem(fetchFalso).get('clienteId')).toBe('7'))
    expect(ultimaListagem(fetchFalso).get('page')).toBe('1')
    expect(parametrosDaUrl().get('clienteId')).toBe('7')
    expect(parametrosDaUrl().get('page') ?? '1').toBe('1')
  })

  it('removes the client filter when all clients is chosen', async () => {
    const fetchFalso = instalar()
    const { user } = renderizarComProvedores(<PedidosPage />, { rota: '/?clienteId=7' })
    const filtro = await aguardarClientes()

    await user.selectOptions(filtro, within(filtro).getByRole('option', { name: 'Todos os clientes' }))

    await waitFor(() => expect(parametrosDaUrl().has('clienteId')).toBe(false))
    await waitFor(() => expect(ultimaListagem(fetchFalso).has('clienteId')).toBe(false))
  })

  it('filters by period, writes it to the URL and goes back to the first page', async () => {
    const fetchFalso = instalar(paginada(60))
    renderizarComProvedores(<PedidosPage />, { rota: '/?page=2' })
    await waitFor(() => expect(fetchFalso.chamadasDe('GET', '/api/pedidos').length).toBeGreaterThan(0))

    fireEvent.change(screen.getByLabelText('Data inicial'), { target: { value: '2026-02-01' } })
    fireEvent.change(screen.getByLabelText('Data final'), { target: { value: '2026-02-28' } })

    await waitFor(() => {
      const parametros = ultimaListagem(fetchFalso)
      expect(parametros.get('dataInicio')).toBe('2026-02-01')
      expect(parametros.get('dataFim')).toBe('2026-02-28')
      expect(parametros.get('page')).toBe('1')
    })
    expect(parametrosDaUrl().get('dataInicio')).toBe('2026-02-01')
    expect(parametrosDaUrl().get('dataFim')).toBe('2026-02-28')
    expect(parametrosDaUrl().get('page') ?? '1').toBe('1')
  })

  it('clears every filter', async () => {
    const fetchFalso = instalar(paginada(60))
    const { user } = renderizarComProvedores(<PedidosPage />, {
      rota: '/?clienteId=7&dataInicio=2026-01-01&dataFim=2026-01-31&page=2',
    })
    const filtro = await aguardarClientes()

    const [limpar] = screen.getAllByRole('button', { name: 'Limpar filtros' })
    if (!limpar) throw new Error('botão ausente')
    await user.click(limpar)

    await waitFor(() => {
      const parametros = ultimaListagem(fetchFalso)
      expect(parametros.has('clienteId')).toBe(false)
      expect(parametros.has('dataInicio')).toBe(false)
      expect(parametros.has('dataFim')).toBe(false)
      expect(parametros.get('page')).toBe('1')
    })
    expect(parametrosDaUrl().has('clienteId')).toBe(false)
    expect(parametrosDaUrl().has('dataInicio')).toBe(false)
    expect(parametrosDaUrl().has('dataFim')).toBe(false)
    expect(filtro).toHaveDisplayValue('Todos os clientes')
    expect(screen.getByLabelText('Data inicial')).toHaveValue('')
    expect(screen.getByLabelText('Data final')).toHaveValue('')
  })

  it('shows the page position and the total of orders', async () => {
    instalar(paginada(45))

    renderizarComProvedores(<PedidosPage />)

    expect(await screen.findByText(/Página 1 de 3/)).toBeInTheDocument()
    expect(screen.getByText(/\b45 pedidos\b/i)).toBeInTheDocument()
  })

  it('navigates to the next and previous pages through the URL', async () => {
    const fetchFalso = instalar(paginada(45))
    const { user } = renderizarComProvedores(<PedidosPage />)
    await screen.findByText(/Página 1 de 3/)

    expect(screen.getByRole('button', { name: 'Anterior' })).toBeDisabled()
    await user.click(screen.getByRole('button', { name: 'Próxima' }))

    expect(await screen.findByText(/Página 2 de 3/)).toBeInTheDocument()
    expect(ultimaListagem(fetchFalso).get('page')).toBe('2')
    expect(parametrosDaUrl().get('page')).toBe('2')

    await user.click(screen.getByRole('button', { name: 'Anterior' }))

    expect(await screen.findByText(/Página 1 de 3/)).toBeInTheDocument()
    expect(ultimaListagem(fetchFalso).get('page')).toBe('1')
  })

  it('keeps the filters when changing page', async () => {
    const fetchFalso = instalar(paginada(45))
    const { user } = renderizarComProvedores(<PedidosPage />, { rota: '/?clienteId=7&dataInicio=2026-01-01&dataFim=2026-01-31' })
    await screen.findByText(/Página 1 de 3/)

    await user.click(screen.getByRole('button', { name: 'Próxima' }))

    await waitFor(() => expect(ultimaListagem(fetchFalso).get('page')).toBe('2'))
    const parametros = ultimaListagem(fetchFalso)
    expect(parametros.get('clienteId')).toBe('7')
    expect(parametros.get('dataInicio')).toBe('2026-01-01')
    expect(parametros.get('dataFim')).toBe('2026-01-31')
  })

  it('disables the next page on the last page', async () => {
    instalar(paginada(45))

    renderizarComProvedores(<PedidosPage />, { rota: '/?page=3' })

    expect(await screen.findByText(/Página 3 de 3/)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Próxima' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Anterior' })).toBeEnabled()
  })

  it('shows a loading status while the orders are requested', async () => {
    const espera = pendente()
    instalar(() => espera.promessa)

    renderizarComProvedores(<PedidosPage />)

    const carregando = await screen.findByText(/Carregando pedidos(…|\.\.\.)/)
    expect(carregando.closest('[role="status"]')).not.toBeNull()
  })

  it('shows the empty state when there are no orders', async () => {
    instalar(() => respostaJson(200, umaPagina([], { totalItems: 0, totalPages: 0 })))

    renderizarComProvedores(<PedidosPage />)

    expect(await screen.findByText('Nenhum pedido encontrado')).toBeInTheDocument()
  })

  it('offers to clear the filters when no order matches them', async () => {
    const fetchFalso = instalar((requisicao) =>
      requisicao.url.searchParams.has('clienteId')
        ? respostaJson(200, umaPagina([], { totalItems: 0, totalPages: 0 }))
        : respostaJson(200, umaPagina(pedidos)),
    )
    const { user } = renderizarComProvedores(<PedidosPage />, { rota: '/?clienteId=7' })

    expect(await screen.findByText('Nenhum pedido encontrado')).toBeInTheDocument()
    const botoes = screen.getAllByRole('button', { name: 'Limpar filtros' })
    const ultimo = botoes[botoes.length - 1]
    if (!ultimo) throw new Error('botão ausente')
    await user.click(ultimo)

    await screen.findByText('Bruno Souza', { selector: 'td, td *' })
    expect(ultimaListagem(fetchFalso).has('clienteId')).toBe(false)
    expect(screen.queryByText('Nenhum pedido encontrado')).not.toBeInTheDocument()
  })

  it('shows the error with the support code and retries on request', async () => {
    let tentativas = 0
    const fetchFalso = instalar(() => {
      tentativas += 1
      return tentativas === 1
        ? respostaProblema(500, umProblema(500, 'erro_interno', { traceId: 'trace-listagem-1', detail: 'SqlException at dbo.Pedidos' }))
        : respostaJson(200, umaPagina(pedidos))
    })
    const { user } = renderizarComProvedores(<PedidosPage />)

    const alerta = await screen.findByRole('alert')
    expect(alerta).toHaveTextContent('Código de suporte: trace-listagem-1')
    expect(alerta).not.toHaveTextContent(/SqlException|dbo\./)
    await user.click(within(alerta).getByRole('button', { name: 'Tentar de novo' }))

    await screen.findByText('Bruno Souza', { selector: 'td, td *' })
    expect(fetchFalso.chamadasDe('GET', '/api/pedidos')).toHaveLength(2)
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })

  it('shows the no connection error when the network fails', async () => {
    instalar(() => falhaDeRede())

    renderizarComProvedores(<PedidosPage />)

    const alerta = await screen.findByRole('alert')
    expect(alerta).toHaveTextContent(/conex|internet|rede|servidor/i)
    expect(within(alerta).getByRole('button', { name: 'Tentar de novo' })).toBeInTheDocument()
  })

  it('shows the inverted period message from the dictionary', async () => {
    const erroDoPeriodo = { campo: 'dataFim', code: 'periodo_invertido', mensagem: 'A data final é anterior à inicial.' }
    instalar((requisicao) => {
      const inicio = requisicao.url.searchParams.get('dataInicio')
      const fim = requisicao.url.searchParams.get('dataFim')
      return inicio && fim && inicio > fim
        ? respostaProblema(400, umProblema(400, 'validacao_falhou', { errors: [erroDoPeriodo] }))
        : respostaJson(200, umaPagina(pedidos))
    })
    renderizarComProvedores(<PedidosPage />)
    await screen.findByText('Bruno Souza', { selector: 'td, td *' })

    fireEvent.change(screen.getByLabelText('Data inicial'), { target: { value: '2026-02-10' } })
    fireEvent.change(screen.getByLabelText('Data final'), { target: { value: '2026-02-01' } })

    const mensagens = await screen.findAllByText(mensagemDoCampo(erroDoPeriodo), { exact: false })
    expect(mensagens.length).toBeGreaterThan(0)
  })

  it('opens the new order dialog', async () => {
    instalar()
    const { user } = renderizarComProvedores(<PedidosPage />)
    await screen.findByText('Bruno Souza', { selector: 'td, td *' })

    await user.click(screen.getByRole('button', { name: 'Novo pedido' }))

    expect(await screen.findByRole('dialog', { name: 'Novo pedido' })).toBeInTheDocument()
  })
})
