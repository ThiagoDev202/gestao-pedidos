# Política: frontend

- **Stack**: React + TypeScript `strict` + Vite, Tailwind + shadcn/ui, TanStack Query, react-hook-form + zod.
- **Estrutura**: `src/features/<feature>/{api,components,hooks,schemas,pages}`, `src/shared/{api,components/ui,lib}`, `src/app`.
- **Fronteira**: chamadas HTTP só em `api/` via `shared/api/http-client`. Páginas compõem componentes; sem `fetch` em componente.
- **Tokens**: cores, raios, sombras e tipografia por CSS vars do Tailwind/shadcn. Proibido hex, `rgb(`, `px` soltos e `style={{}}` para visual.
- **Componentes**: reutilize shadcn/ui (`shared/components/ui`); não duplique botão, input, dialog, tabela.
- **Estados obrigatórios**: vazio, carregando, erro (com "tentar de novo") e sucesso.
- **Formulários**: validação zod com mensagem em pt-BR junto do campo; erro 422 da API exibido; botão de envio desabilitado enquanto envia; foco no primeiro erro.
- **Erros**: padrão em `docs/policies/errors.md` (`ApiError`, dicionário `code → mensagem`, `ErrorAlert`/`FieldError`).
- **Servidor é a fonte**: o front valida formato; regra de negócio (estoque, cliente) é do backend e o erro dele é exibido, não reimplementado.
- **Listagem**: filtros e paginação nos search params da URL; chave de query inclui filtros.
- **i18n**: textos em pt-BR; moeda `Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })`; datas recebidas em UTC e exibidas no fuso local.
- **Acessibilidade**: `label` em todo input, `aria-label` em ícone clicável, dialog com título, teclado completo, contraste suficiente.
- **TypeScript**: sem `any`; tipos de API inferidos de schemas zod.
