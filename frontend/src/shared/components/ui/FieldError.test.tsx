import { render, screen } from '@testing-library/react'
import { FieldError } from '@/shared/components/ui/FieldError'

describe('FieldError', () => {
  it('renders nothing without a message', () => {
    const { container } = render(<FieldError />)

    expect(container).toBeEmptyDOMElement()
  })

  it('renders nothing with an empty message', () => {
    const { container } = render(<FieldError mensagem="" />)

    expect(container).toBeEmptyDOMElement()
  })

  it('renders the message as an alert paragraph with the given id', () => {
    render(<FieldError mensagem="Informe uma quantidade maior que zero." id="quantidade-erro" />)

    const alerta = screen.getByRole('alert')
    expect(alerta.tagName).toBe('P')
    expect(alerta).toHaveTextContent('Informe uma quantidade maior que zero.')
    expect(alerta).toHaveAttribute('id', 'quantidade-erro')
  })

  it('can describe the field it belongs to', () => {
    render(
      <>
        <input aria-label="Quantidade" aria-describedby="quantidade-erro" />
        <FieldError mensagem="Informe uma quantidade maior que zero." id="quantidade-erro" />
      </>,
    )

    expect(screen.getByLabelText('Quantidade')).toHaveAccessibleDescription('Informe uma quantidade maior que zero.')
  })
})
