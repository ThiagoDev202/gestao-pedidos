<!--
Sync Impact Report
Versão: 1.0.0 → 1.0.1 (PATCH: problemas fora do escopo passam a virar issues do GitHub, em vez de docs/known-issues.md, removido)
Templates sincronizados no 1.0.1: spec, plan e tasks em pt-BR com os gates do projeto
Versão anterior: (modelo) → 1.0.0 (ratificação inicial)
Princípios adicionados: I–VII; seções Idioma, Restrições técnicas, Fluxo e gates de qualidade, Governança
Pendências: nenhuma
-->
# Constituição do Gestão de Pedidos

Aplicação de cadastro e consulta de pedidos de clientes. Duas camadas no mesmo repositório: `backend/` (.NET, Clean Architecture, SQL Server) e `frontend/` (React + TypeScript). Banco versionado em `database/` e orquestração em `docker-compose.yml`.

Contexto obrigatório antes de `/speckit-specify` e `/speckit-plan`: `docs/domain.md` e o doc da área afetada (índice em `CLAUDE.md`).

## Idioma do projeto (NÃO NEGOCIÁVEL)

| Artefato | Idioma |
|---|---|
| `specs/**`, `docs/**`, ADRs, políticas, constitution, README | **pt-BR** |
| Textos de interface e mensagens de erro exibidas ao usuário | **pt-BR** |
| Linguagem ubíqua do domínio no código (`Pedido`, `Cliente`, `Produto`, `ItemPedido`, `CriarPedidoHandler`) | **pt-BR** |
| Termos técnicos (`Repository`, `Handler`, `Validator`, `Endpoints`), rotas de infraestrutura, logs e commits | **inglês** |

## Princípios

### I. O código é a fonte da verdade
Documentação é hipótese até ser confirmada no código. Toda spec e todo plano MUST citar os arquivos que sustentam suas afirmações sobre o sistema atual (`caminho:linha`). Quando doc e código divergirem, MUST corrigir o doc na mesma entrega. Afirmação não verificada MUST ser marcada como "não verificado".

### II. Spec-Driven no tamanho certo, com aprovação humana
- Feature nova, mudança de contrato da API e mudança de schema MUST seguir `/speckit-specify` → `/speckit-clarify` → `/speckit-plan` → `/speckit-tasks` → `/speckit-analyze` → `/speckit-implement`.
- Correção que não muda contrato da API (rota, campo, obrigatoriedade, validação, formato, status ou mensagem), nem schema, dentro de uma camada (trilha leve) MAY dispensar spec, com o comportamento esperado e a causa raiz no corpo do PR, mas MUST passar pelos gates.
- O agente MUST parar e aguardar aprovação humana: após a spec, após plano + tarefas e antes de commit/push. Commit e push MUST NOT acontecer sem pedido explícito.

### III. Contrato da API é explícito e único
`specs/<NNN>/contracts/openapi.yaml` é a fonte do contrato. Toda mudança em rota, DTO ou formato de resposta MUST atualizar o contrato, o cliente em `frontend/src/features/*/api` e os testes de integração. Erros seguem ProblemDetails (RFC 9457): 400 para validação estrutural, 422 para regra de negócio, 404 para recurso inexistente.

### IV. Clean Architecture e SOLID verificados por teste
- Dependências apontam para dentro: `Domain` não referencia ninguém; `Application` referencia só `Domain`; `Infrastructure` e `Api` referenciam `Application`. Um teste de arquitetura (`backend/tests/GestaoPedidos.ArchitectureTests`) MUST falhar se a regra for quebrada.
- Escrita (Commands) passa pelo agregado de domínio e pelo EF Core; leitura (Queries) usa SQL explícito via Dapper e devolve DTOs (CQRS leve, mesmo banco, sem MediatR).
- Regras de negócio vivem no `Domain` e na `Application`, nunca em endpoint, repositório ou componente React.

### V. Teste antes do código novo
- Testes MUST ser escritos e aprovados **somente pelo agente `qa-guardian`**, a partir dos requisitos e critérios de aceite, nunca da implementação. Quem implementa MUST NOT alterar testes; ajusta o código até passarem.
- Regra de negócio nova ou alterada MUST ter teste unitário escrito antes (RED → GREEN). Endpoints MUST ter teste de integração contra SQL Server real (Testcontainers). Fluxos de tela críticos MUST ter teste Playwright.
- Teste existente MUST NOT ser enfraquecido, apagado ou desativado para a entrega passar.

### VI. Banco só por scripts versionados e medido
O schema nasce de `database/*.sql`, idempotentes e executados automaticamente no `docker compose up`. O EF Core MUST NOT criar nem migrar o schema. Índice novo MUST ter justificativa com plano de execução (`STATISTICS IO`, plano real) registrada em `docs/playbooks/query-debug.md` ou no plano.

### VII. Simplicidade e mudança cirúrgica
Fazer o menor conjunto de mudanças que cumpre a spec. Padrão de projeto (GoF) só quando resolve um problema real e está nomeado no plano; abstração, dependência ou configuração que a spec não pede MUST NOT entrar. Problemas fora do escopo encontrados no caminho MUST virar issue do GitHub, não ser corrigidos de carona.

## Restrições técnicas

- Backend: .NET 10 LTS, Minimal APIs, EF Core (escrita) + Dapper (leitura), FluentValidation, Polly (retry de transientes de SQL), rate limiting nativo do ASP.NET Core. Sem circuit breaker (dependência única: o banco; ADR-007). Bibliotecas com licença comercial (MediatR, FluentAssertions) MUST NOT ser usadas.
- Frontend: React + TypeScript (strict) + Vite, Tailwind + shadcn/ui, TanStack Query, react-hook-form + zod. Chamadas HTTP só em `src/shared/api` e `src/features/*/api`.
- Banco: SQL Server 2022 em container. Ambiente local completo: `docker compose up`.
- Segredos, URLs de ambiente e senhas MUST NOT ficar no código: variáveis de ambiente com entrada no `.env.example`.

## Fluxo e gates de qualidade

Uma entrega só é concluída quando:
1. Backend: `dotnet format --verify-no-changes`, `dotnet build -warnaserror` e `dotnet test` (unitários, arquitetura, integração) sem falhas.
2. Frontend: `npm run lint`, `npm run typecheck`, `npm test` e `npm run build` sem erros.
3. `scripts/check-guardrails.sh` sem violações (sem comentários no código, sem `any`, sem segredos).
4. Agente `qa-guardian` com veredito **APROVADO**, `ux-guardian` **APROVADO** em mudança de UI e `code-reviewer` sem achado **Bloqueante**.
5. CI do PR verde (checks `ci-ok` e `governance`).
6. Docs afetados atualizados (skill `/retro`).
7. Tarefas marcadas `[x]` em `tasks.md`.

O fluxo de entrega está na skill `/ship`. A `main` é protegida e só recebe PR por **merge commit** (o histórico de commits pequenos é preservado, sem squash nem rebase). Branches `{tipo}/{NNN}-{slug}` e Conventional Commits, conforme `docs/policies/git.md`. Commits só no nome da pessoa responsável.

## Governança

Esta constituição prevalece sobre preferências individuais e atalhos. Emendas via `/speckit-constitution`, com versão semântica: MAJOR remove ou redefine princípio; MINOR acrescenta princípio ou amplia de forma relevante; PATCH esclarece. O guia operacional do dia a dia é o `AGENTS.md`; as políticas detalhadas ficam em `docs/policies/`.

**Versão**: 1.0.1 | **Ratificada**: 2026-11-13 | **Última alteração**: 2026-11-13
