export function FieldError({ mensagem, id }: { mensagem?: string; id?: string }) {
  if (!mensagem) return null
  return (
    <p id={id} role="alert" className="text-sm text-destructive">
      {mensagem}
    </p>
  )
}
