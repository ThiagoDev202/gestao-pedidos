import { paraCaminhoDoFormulario } from '@/shared/api/caminho-do-campo'

describe('paraCaminhoDoFormulario', () => {
  it.each([
    ['itens[0].quantidade', 'itens.0.quantidade'],
    ['itens[12].produtoId', 'itens.12.produtoId'],
    ['itens[3]', 'itens.3'],
    ['itens', 'itens'],
    ['clienteId', 'clienteId'],
    ['$', 'root'],
  ])('maps the API field %s to the form path %s', (campo, esperado) => {
    expect(paraCaminhoDoFormulario(campo)).toBe(esperado)
  })
})
