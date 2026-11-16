import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ApiError } from '@/shared/api/ApiError'
import { mensagemDoErro } from '@/shared/api/error-messages'
import { ErrorAlert } from '@/shared/components/ui/ErrorAlert'

const traceId = '00-4bf92f3577b34da6a3ce929d0e0e4736-00'

function umErro(status: number, code: string, extra: Partial<ConstructorParameters<typeof ApiError>[0]> = {}): ApiError {
  return new ApiError({ status, code, title: 'Título', detail: 'Detalhe técnico do servidor', traceId, ...extra })
}

describe('ErrorAlert', () => {
  it('renders nothing when there is no error', () => {
    const { container } = render(<ErrorAlert erro={null} />)

    expect(container).toBeEmptyDOMElement()
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })

  it('renders nothing when the error is undefined', () => {
    const { container } = render(<ErrorAlert erro={undefined} />)

    expect(container).toBeEmptyDOMElement()
  })

  it('shows the dictionary message of the error code in an alert', () => {
    const erro = umErro(422, 'estoque_insuficiente', {
      meta: { produtoId: 12, produtoNome: 'Teclado Mecânico', solicitado: 5, disponivel: 2 },
    })

    render(<ErrorAlert erro={erro} />)

    const alerta = screen.getByRole('alert')
    expect(alerta).toHaveTextContent(mensagemDoErro(erro))
    expect(alerta).toHaveTextContent('Teclado Mecânico')
    expect(alerta).not.toHaveTextContent('Detalhe técnico do servidor')
  })

  it.each([
    [500, 'erro_interno'],
    [503, 'banco_indisponivel'],
    [500, 'codigo_que_nao_existe'],
    [418, 'codigo_que_nao_existe'],
  ])('shows the traceId as support code for an unexpected %i %s', (status, code) => {
    render(<ErrorAlert erro={umErro(status, code)} />)

    expect(screen.getByRole('alert')).toHaveTextContent(`Código de suporte: ${traceId}`)
  })

  it('does not show technical detail of a server failure', () => {
    render(<ErrorAlert erro={umErro(500, 'erro_interno', { detail: "SqlException: Invalid object name 'dbo.Pedidos'" })} />)

    expect(screen.getByRole('alert')).not.toHaveTextContent(/SqlException|dbo\./)
  })

  it.each([
    [0, 'sem_conexao'],
    [429, 'limite_requisicoes_excedido'],
    [500, 'erro_interno'],
    [503, 'banco_indisponivel'],
  ])('offers a retry action for the repeatable %i %s', async (status, code) => {
    const user = userEvent.setup()
    const onTentarNovamente = vi.fn()

    render(<ErrorAlert erro={umErro(status, code, { meta: { retryAfterSegundos: 5 }, retryAfterSegundos: 5 })} onTentarNovamente={onTentarNovamente} />)
    await user.click(screen.getByRole('button', { name: 'Tentar de novo' }))

    expect(onTentarNovamente).toHaveBeenCalledTimes(1)
  })

  it.each([
    [422, 'estoque_insuficiente'],
    [422, 'cliente_inexistente'],
    [400, 'validacao_falhou'],
  ])('does not offer a retry action for the non repeatable %i %s', (status, code) => {
    render(<ErrorAlert erro={umErro(status, code)} onTentarNovamente={vi.fn()} />)

    expect(screen.getByRole('alert')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Tentar de novo' })).not.toBeInTheDocument()
  })

  it('does not offer a retry action without a retry handler', () => {
    render(<ErrorAlert erro={umErro(500, 'erro_interno')} />)

    expect(screen.queryByRole('button', { name: 'Tentar de novo' })).not.toBeInTheDocument()
  })

  it('shows how long to wait on a 429', () => {
    render(
      <ErrorAlert
        erro={umErro(429, 'limite_requisicoes_excedido', { meta: { retryAfterSegundos: 30 }, retryAfterSegundos: 30 })}
        onTentarNovamente={vi.fn()}
      />,
    )

    expect(screen.getByRole('alert')).toHaveTextContent(/30\s*segundo/i)
  })

  it('shows the no connection message when the network fails', () => {
    const erro = new ApiError({ status: 0, code: 'sem_conexao', title: 'Sem conexão', detail: 'Failed to fetch' })

    render(<ErrorAlert erro={erro} />)

    expect(screen.getByRole('alert')).toHaveTextContent(mensagemDoErro(erro))
    expect(screen.getByRole('alert')).not.toHaveTextContent('Failed to fetch')
  })
})
