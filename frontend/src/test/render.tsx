import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { ReactElement } from 'react'
import { MemoryRouter, useLocation } from 'react-router-dom'

function SondaDeLocalizacao() {
  const localizacao = useLocation()
  return <div data-testid="localizacao">{`${localizacao.pathname}${localizacao.search}`}</div>
}

export function renderizarComProvedores(ui: ReactElement, { rota = '/' }: { rota?: string } = {}) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false, gcTime: Infinity }, mutations: { retry: false } },
  })
  const user = userEvent.setup()
  const resultado = render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[rota]}>
        {ui}
        <SondaDeLocalizacao />
      </MemoryRouter>
    </QueryClientProvider>,
  )
  return { ...resultado, queryClient, user }
}

export function parametrosDaUrl(): URLSearchParams {
  const texto = screen.getByTestId('localizacao').textContent ?? ''
  const indice = texto.indexOf('?')
  return new URLSearchParams(indice >= 0 ? texto.slice(indice + 1) : '')
}
