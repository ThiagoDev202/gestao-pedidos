import { zodResolver } from '@hookform/resolvers/zod'
import { Plus, Trash2 } from 'lucide-react'
import { useEffect, type RefObject } from 'react'
import { useFieldArray, useForm, useWatch } from 'react-hook-form'
import { useClientes, useProdutos } from '@/features/pedidos/hooks/usePedidos'
import { useCriarPedido } from '@/features/pedidos/hooks/useCriarPedido'
import {
  formularioVazio,
  MAXIMO_DE_ITENS,
  novoPedidoSchema,
  type NovoPedidoFormulario,
} from '@/features/pedidos/schemas/novo-pedido'
import type { PedidoDetalhe } from '@/shared/api/types'
import { ApiError } from '@/shared/api/ApiError'
import { paraCaminhoDoFormulario } from '@/shared/api/caminho-do-campo'
import { mensagemDoCampo } from '@/shared/api/error-messages'
import { Button } from '@/shared/components/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/shared/components/ui/dialog'
import { ErrorAlert } from '@/shared/components/ui/ErrorAlert'
import { FieldError } from '@/shared/components/ui/FieldError'
import { Input } from '@/shared/components/ui/input'
import { Label } from '@/shared/components/ui/label'
import { Select } from '@/shared/components/ui/select'
import { formatarMoeda } from '@/shared/lib/format'

interface NovoPedidoDialogProps {
  open: boolean
  onOpenChange: (aberto: boolean) => void
  onCriado?: (pedido: PedidoDetalhe) => void
  retornarFoco?: RefObject<HTMLElement | null>
}

export function NovoPedidoDialog({ open, onOpenChange, onCriado, retornarFoco }: NovoPedidoDialogProps) {
  const clientes = useClientes()
  const produtos = useProdutos(open)
  const criar = useCriarPedido()

  const formulario = useForm<NovoPedidoFormulario>({
    resolver: zodResolver(novoPedidoSchema),
    defaultValues: formularioVazio,
    shouldFocusError: true,
  })
  const { register, control, handleSubmit, setError, reset, formState } = formulario
  const { fields, append, remove } = useFieldArray({ control, name: 'itens' })
  const itens = useWatch({ control, name: 'itens' })

  useEffect(() => {
    if (open) {
      reset(formularioVazio)
      criar.reset()
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open])

  const catalogo = produtos.data ?? []
  const precoDe = (produtoId: string) => catalogo.find((produto) => String(produto.id) === produtoId)?.preco ?? 0
  const subtotalDe = (indice: number) => {
    const item = itens?.[indice]
    const quantidade = Number(item?.quantidade)
    return item && Number.isInteger(quantidade) && quantidade > 0 ? precoDe(item.produtoId) * quantidade : 0
  }
  const total = (itens ?? []).reduce((soma, _item, indice) => soma + subtotalDe(indice), 0)
  const escolhidos = new Set((itens ?? []).map((item) => item.produtoId).filter(Boolean))

  const erroDoEnvio = criar.error instanceof ApiError ? criar.error : null
  const erroDeCorpo = erroDoEnvio?.errors.find((erro) => paraCaminhoDoFormulario(erro.campo) === 'root')
  const alertaGeral = erroDoEnvio && !(erroDoEnvio.status === 400 && erroDoEnvio.errors.length > 0) ? erroDoEnvio : null

  async function enviar(valores: NovoPedidoFormulario) {
    try {
      const criado = await criar.mutateAsync({
        clienteId: Number(valores.clienteId),
        itens: valores.itens.map((item) => ({ produtoId: Number(item.produtoId), quantidade: Number(item.quantidade) })),
      })
      onCriado?.(criado)
      onOpenChange(false)
    } catch (erro) {
      if (!(erro instanceof ApiError)) throw erro
      const deCampo = erro.errors.filter((campo) => paraCaminhoDoFormulario(campo.campo) !== 'root')
      deCampo.forEach((campo, posicao) => {
        setError(paraCaminhoDoFormulario(campo.campo) as Parameters<typeof setError>[0], { type: 'server', message: mensagemDoCampo(campo) }, { shouldFocus: posicao === 0 })
      })
    }
  }

  const aoEnviar = handleSubmit(enviar)
  const enviando = criar.isPending

  function descricaoDe(id: string, mensagem?: string) {
    return mensagem ? id : undefined
  }

  const erros = formState.errors

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        onCloseAutoFocus={(evento) => {
          if (retornarFoco?.current) {
            evento.preventDefault()
            retornarFoco.current.focus()
          }
        }}
      >
        <DialogHeader>
          <DialogTitle>Novo pedido</DialogTitle>
          <DialogDescription>Escolha o cliente e adicione os produtos do pedido.</DialogDescription>
        </DialogHeader>

        <form onSubmit={(evento) => void aoEnviar(evento)} className="flex flex-col gap-4" noValidate>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="novo-pedido-cliente">Cliente</Label>
            {clientes.error instanceof ApiError ? (
              <ErrorAlert erro={clientes.error} onTentarNovamente={() => void clientes.refetch()} />
            ) : null}
            <Select
              id="novo-pedido-cliente"
              aria-invalid={Boolean(erros.clienteId)}
              aria-describedby={descricaoDe('novo-pedido-cliente-erro', erros.clienteId?.message)}
              {...register('clienteId')}
            >
              <option value="">Selecione um cliente</option>
              {(clientes.data ?? []).map((cliente) => (
                <option key={cliente.id} value={cliente.id}>
                  {cliente.nome}
                </option>
              ))}
            </Select>
            <FieldError id="novo-pedido-cliente-erro" mensagem={erros.clienteId?.message} />
          </div>

          <fieldset className="flex flex-col gap-3">
            <legend className="mb-1 text-sm font-medium">Itens</legend>
            {produtos.error instanceof ApiError ? (
              <ErrorAlert erro={produtos.error} onTentarNovamente={() => void produtos.refetch()} />
            ) : null}
            {fields.map((campo, indice) => {
              const erroProduto = erros.itens?.[indice]?.produtoId?.message
              const erroQuantidade = erros.itens?.[indice]?.quantidade?.message
              const idProduto = `item-${indice}-produto`
              const idQuantidade = `item-${indice}-quantidade`
              return (
                <div key={campo.id} className="grid grid-cols-1 items-start gap-3 sm:grid-cols-[1fr_8rem_auto]">
                  <div className="flex flex-col gap-1.5">
                    <Label htmlFor={idProduto}>{`Produto do item ${indice + 1}`}</Label>
                    <Select
                      id={idProduto}
                      aria-invalid={Boolean(erroProduto)}
                      aria-describedby={descricaoDe(`${idProduto}-erro`, erroProduto)}
                      {...register(`itens.${indice}.produtoId`)}
                    >
                      <option value="">Selecione um produto</option>
                      {catalogo.map((produto) => {
                        const emOutraLinha = escolhidos.has(String(produto.id)) && itens?.[indice]?.produtoId !== String(produto.id)
                        return (
                          <option key={produto.id} value={produto.id} disabled={emOutraLinha}>
                            {`${produto.nome} (estoque: ${produto.estoque})`}
                          </option>
                        )
                      })}
                    </Select>
                    <FieldError id={`${idProduto}-erro`} mensagem={erroProduto} />
                  </div>
                  <div className="flex flex-col gap-1.5">
                    <Label htmlFor={idQuantidade}>{`Quantidade do item ${indice + 1}`}</Label>
                    <Input
                      id={idQuantidade}
                      type="number"
                      inputMode="numeric"
                      aria-invalid={Boolean(erroQuantidade)}
                      aria-describedby={descricaoDe(`${idQuantidade}-erro`, erroQuantidade)}
                      {...register(`itens.${indice}.quantidade`)}
                    />
                    <FieldError id={`${idQuantidade}-erro`} mensagem={erroQuantidade} />
                  </div>
                  <div className="flex items-center justify-between gap-2 sm:flex-col sm:items-end sm:justify-start sm:pt-6">
                    <span className="text-sm text-muted-foreground">{formatarMoeda(subtotalDe(indice))}</span>
                    <Button
                      variant="ghost"
                      size="icon"
                      aria-label={`Remover item ${indice + 1}`}
                      disabled={fields.length <= 1}
                      onClick={() => remove(indice)}
                    >
                      <Trash2 className="h-4 w-4" aria-hidden="true" />
                    </Button>
                  </div>
                </div>
              )
            })}
            <FieldError mensagem={typeof erros.itens?.message === 'string' ? erros.itens.message : erros.itens?.root?.message} />
            <div>
              <Button
                variant="outline"
                size="sm"
                disabled={fields.length >= MAXIMO_DE_ITENS}
                onClick={() => append({ produtoId: '', quantidade: '1' })}
              >
                <Plus className="h-4 w-4" aria-hidden="true" />
                Adicionar item
              </Button>
            </div>
          </fieldset>

          <p className="text-right text-base font-semibold">
            Total: <span>{formatarMoeda(total)}</span>
          </p>

          <FieldError mensagem={erroDeCorpo ? mensagemDoCampo(erroDeCorpo) : undefined} />
          <ErrorAlert erro={alertaGeral} onTentarNovamente={() => void aoEnviar()} />

          <DialogFooter>
            <Button variant="outline" onClick={() => onOpenChange(false)}>
              Cancelar
            </Button>
            <Button type="submit" disabled={enviando}>
              Criar pedido
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
