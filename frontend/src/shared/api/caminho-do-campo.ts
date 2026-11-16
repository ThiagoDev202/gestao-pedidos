export function paraCaminhoDoFormulario(campo: string): string {
  if (campo === '$') return 'root'
  return campo.replace(/\[(\d+)\]/g, '.$1')
}
