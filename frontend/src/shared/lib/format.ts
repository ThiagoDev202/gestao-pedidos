const moeda = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })
const dataHora = new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' })

export function formatarMoeda(valor: number) {
  return moeda.format(valor)
}

export function formatarDataHora(utc: string) {
  return dataHora.format(new Date(utc))
}
