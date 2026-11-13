---
name: new-screen
description: Guia a criação de uma tela ou modal nova no frontend do Gestão de Pedidos no padrão do projeto (feature folder, api, schema zod, hook TanStack Query, componentes shadcn/ui, estados e testes). Use quando a spec pedir uma tela, formulário, modal ou listagem nova.
---

# /new-screen — tela nova no padrão do projeto

Pré-requisitos: spec aprovada com os estados da tela e o endpoint já descrito no contrato (`/new-endpoint`).

## 1. Testes primeiro
`qa-guardian` (RED): Vitest + Testing Library para a tela (estados vazio/carregando/erro/sucesso, validação zod, erro 422 exibido, botão desabilitado no envio) e, para fluxo crítico, o cenário Playwright. `ux-guardian` antes: componentes e tokens a reutilizar.

## 2. Estrutura
```
frontend/src/features/<feature>/
  api/<recurso>.ts          funções HTTP (único lugar que chama a API) usando shared/api/http-client
  schemas/<caso>.ts         schemas zod e tipos inferidos
  hooks/use<Caso>.ts        TanStack Query: useQuery / useMutation + invalidação
  components/<Componente>.tsx
  pages/<Pagina>.tsx        compõe componentes; default export
```
- Componentes base em `shared/components/ui` (shadcn/ui); adicione com o CLI do shadcn, não copie à mão.
- Estilo só com classes Tailwind e tokens; sem cor ou tamanho fixos; textos em pt-BR.
- Filtros e paginação refletidos na URL (search params); chaves de query incluem os filtros.
- Formulário: react-hook-form + `zodResolver`; erro 422/400 do ProblemDetails mapeado para campo ou mensagem geral; submit desabilitado enquanto envia; invalide as queries afetadas no sucesso.
- Sem `any`; sem regra de negócio duplicada do backend (só validação de formato/UX).

## 3. Fechamento
`qa-guardian` e `ux-guardian` (APROVADO), `code-reviewer`, gates do front, `/ship` e `/retro`.
