import type { ApiError } from '@/shared/api/ApiError'
import type { ErroDeCampo } from '@/shared/api/types'

type Meta = Record<string, unknown>

function texto(meta: Meta, chave: string, padrao: string) {
  const valor = meta[chave]
  return typeof valor === 'string' || typeof valor === 'number' ? String(valor) : padrao
}

function espera(erro: ApiError) {
  const valor = erro.retryAfterSegundos ?? Number(erro.meta.retryAfterSegundos)
  if (!Number.isFinite(valor) || valor <= 0) return 'alguns segundos'
  return `${valor} ${valor === 1 ? 'segundo' : 'segundos'}`
}

const mensagensPorCodigo: Record<string, (erro: ApiError) => string> = {
  validacao_falhou: () => 'Alguns dados estão inválidos. Corrija os campos indicados e tente de novo.',
  pedido_nao_encontrado: ({ meta }) => `O pedido ${texto(meta, 'pedidoId', 'informado')} não foi encontrado.`,
  recurso_nao_encontrado: () => 'O recurso solicitado não foi encontrado.',
  formato_nao_suportado: () => 'O formato enviado não é suportado pelo servidor.',
  cliente_inexistente: ({ meta }) => `O cliente ${texto(meta, 'clienteId', 'selecionado')} não existe mais. Escolha outro cliente.`,
  produto_inexistente: ({ meta }) => `O produto ${texto(meta, 'produtoId', 'selecionado')} não existe mais. Escolha outro produto.`,
  estoque_insuficiente: ({ meta }) =>
    `Só há ${texto(meta, 'disponivel', 'poucas')} unidade(s) de ${texto(meta, 'produtoNome', 'um dos produtos')} em estoque; você pediu ${texto(meta, 'solicitado', 'mais')}. Ajuste a quantidade e tente de novo.`,
  chave_reutilizada: () => 'Este envio já foi usado para outro pedido. Revise os dados e envie novamente como um novo pedido.',
  limite_requisicoes_excedido: (erro) => `Muitas requisições em pouco tempo. Aguarde ${espera(erro)} e tente de novo.`,
  erro_interno: () => 'Ocorreu um erro inesperado. Tente de novo em instantes.',
  banco_indisponivel: () => 'O serviço está temporariamente indisponível. Tente de novo em instantes.',
  sem_conexao: () => 'Não foi possível conectar ao servidor. Verifique sua conexão com a internet e tente de novo.',
  resposta_invalida: () => 'O servidor enviou uma resposta inesperada. Tente de novo em instantes.',
}

function mensagemGenerica(status: number) {
  if (status === 404) return 'O recurso solicitado não foi encontrado.'
  if (status === 429) return 'Muitas requisições em pouco tempo. Aguarde um pouco e tente de novo.'
  if (status >= 500) return 'O servidor não conseguiu concluir a operação. Tente de novo em instantes.'
  if (status === 401 || status === 403) return 'Você não tem permissão para realizar esta operação.'
  return 'Não foi possível concluir a operação. Revise os dados e tente de novo.'
}

export function mensagemDoErro(erro: ApiError): string {
  const montar = mensagensPorCodigo[erro.code]
  return montar ? montar(erro) : mensagemGenerica(erro.status)
}

const mensagensDeCampo: Record<string, string> = {
  corpo_invalido: 'Os dados enviados não estão em um formato válido.',
  cliente_id_invalido: 'Selecione um cliente válido.',
  itens_vazio: 'Adicione pelo menos um item ao pedido.',
  itens_excedem_limite: 'Um pedido pode ter no máximo 50 itens.',
  produto_id_invalido: 'Selecione um produto válido.',
  quantidade_invalida: 'Informe uma quantidade inteira maior que zero.',
  produto_repetido: 'Este produto já está no pedido. Altere a quantidade da linha existente.',
  chave_idempotencia_invalida: 'Não foi possível identificar este envio. Tente de novo.',
  pagina_invalida: 'A página informada não existe.',
  tamanho_pagina_invalido: 'O tamanho de página deve estar entre 1 e 100.',
  data_invalida: 'Informe uma data válida.',
  periodo_obrigatorio: 'Informe a data inicial e a data final do período.',
  periodo_invertido: 'A data inicial não pode ser depois da data final.',
}

export function mensagemDoCampo(erro: ErroDeCampo): string {
  return mensagensDeCampo[erro.code] ?? 'O valor informado é inválido.'
}
