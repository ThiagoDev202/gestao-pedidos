---
description: "Lista de tarefas da feature Mini Gestão de Pedidos"
---

# Tarefas: Mini Gestão de Pedidos

**Entrada**: `specs/001-gestao-pedidos/` (spec, plan, research, data-model, contracts/openapi.yaml, quickstart)
**Pré-requisitos**: plan.md e spec.md (prontos)

**Testes**: obrigatórios (Constituição V). O `qa-guardian` escreve os testes **antes** do código, confirma que falham pelo motivo certo, lacra com `scripts/qa-seal.sh seal` e há um commit `test(...)` vermelho antes do `feat(...)`. Quem implementa não edita testes.

## Formato: `- [ ] T001 [P?] [US?] Descrição com caminho do arquivo`

- **[P]**: paralelizável (arquivos diferentes, sem dependência pendente)
- **[USn]**: história da spec (US1 criar pedido, US2 listar, US3 top clientes, US4 criar pela tela, US5 consultas de apoio)
- Sem comentários no código; domínio em pt-BR, termos técnicos em inglês; erros pelo padrão de `docs/policies/errors.md`
- Branch de cada PR: `feat/001-backend-pedidos` (PR 2), `feat/002-frontend-pedidos` (PR 3), `perf/003-query-ranking-e-entrega` (PR 4). O PR 1 (SDD) é a branch atual.

---

# PR 1 — SDD (branch `docs/001-sdd-gestao-pedidos`)

- [x] T001 Especificar a feature em `specs/001-gestao-pedidos/spec.md` (2 clarificações resolvidas com o usuário)
- [x] T002 Planejar: `plan.md`, `research.md`, `data-model.md`, `contracts/openapi.yaml`, `quickstart.md`
- [x] T003 Definir o padrão de erros em `docs/policies/errors.md` e ADR-009
- [x] T004 Gerar este `tasks.md`
- [ ] T005 `/speckit-analyze` sobre spec, plano e tarefas; corrigir inconsistências
- [ ] T006 Consolidar `docs/spec.md` (objetivo, regras, endpoints, modelo de dados, critérios de aceite) apontando para `specs/001-gestao-pedidos/`
- [ ] T007 Escrever `docs/plan.md`: plano gerado em plan mode + plano do Spec Kit + seção "O que ajustei ou discordei e por quê"
- [ ] T008 Gates do PR 1: `code-reviewer` (modo docs), `/retro`, `scripts/check-guardrails.sh --diff origin/main`, `scripts/check-docs-drift.sh`, abrir PR `docs(ai): ...` e merge por merge commit

---

# PR 2 — Backend (branch `feat/001-backend-pedidos`)

## Fase 1: Preparação

- [ ] T009 [P] Criar `database/01-create-database.sql` (cria o banco `GestaoPedidos` se não existir, idempotente)
- [ ] T010 [P] Criar `backend/GestaoPedidos.slnx`, `Directory.Build.props` (`Nullable` enable, `TreatWarningsAsErrors`, analyzers, `LangVersion` latest) e `Directory.Packages.props` (Central Package Management) em `backend/`
- [ ] T011 Criar os 4 projetos em `backend/src/` (`GestaoPedidos.Domain`, `.Application`, `.Infrastructure`, `.Api`) com as referências da Clean Architecture (Domain sem referências; Application → Domain; Infrastructure → Application; Api → Application e Infrastructure)
- [ ] T012 Criar os 3 projetos de teste em `backend/tests/` (`GestaoPedidos.UnitTests`, `.ArchitectureTests`, `.IntegrationTests`) com xUnit, Shouldly, NSubstitute, NetArchTest.Rules, Testcontainers.MsSql, Respawn, Microsoft.AspNetCore.Mvc.Testing
- [ ] T013 [P] Criar `.editorconfig` de C# em `backend/.editorconfig` (regras de estilo e analisadores como erro) e confirmar `dotnet format backend --verify-no-changes`
- [ ] T014 [P] Criar `backend/Dockerfile` (multi-stage sdk:10.0 → aspnet:10.0, usuário não root) e `backend/.dockerignore`
- [ ] T015 [P] Criar `database/init/run-scripts.sh` (aguarda o SQL, executa `database/*.sql` em ordem com `sqlcmd -C`, sai com 0) e `docker-compose.yml` com `sqlserver` (2022, healthcheck) e `db-init` (`depends_on: service_healthy`)

## Fase 2: Fundação (bloqueia todas as histórias)

### Banco
- [ ] T016 Criar `database/02-schema.sql` idempotente conforme `data-model.md`: `Clientes`, `Produtos`, `Pedidos`, `ItensPedido`, `IdempotenciaPedidos` com PK, FK, `UNIQUE` (`Clientes.Email`, `Produtos.Sku`, `ItensPedido(PedidoId, ProdutoId)`), `CHECK` (`Preco >= 0`, `Estoque >= 0`, `Quantidade > 0`, `PrecoUnitario >= 0`, `ValorTotal >= 0`) e os 4 índices do plano
- [ ] T017 Criar `database/03-seed.sql` set-based e determinístico (~200 clientes com homônimos, ~100 produtos com estoques variados e um com 1 unidade, ~50 mil pedidos em 2025–2026 com 1 a 5 itens e `ValorTotal` consistente), idempotente
- [ ] T018 Verificar `docker compose up sqlserver db-init` aplicando os scripts duas vezes sem erro nem duplicação

### Testes da fundação (qa-guardian, RED)
- [ ] T019 [P] Teste de arquitetura em `backend/tests/GestaoPedidos.ArchitectureTests/DependencyRulesTests.cs`: Domain sem dependências; Application só em Domain; Api sem EF Core/Dapper fora do composition root; Domain e Application sem `Microsoft.AspNetCore`
- [ ] T020 [P] Testes do catálogo de erros em `backend/tests/GestaoPedidos.UnitTests/Errors/ErrorCatalogTests.cs`: toda exceção de domínio e todo `code` de `docs/policies/errors.md` mapeia para `status`, `title`, `code`, `detail` e `meta`; nenhum `detail` expõe mensagem interna
- [ ] T021 Infra de teste de integração em `backend/tests/GestaoPedidos.IntegrationTests/Support/` (`SqlServerFixture` com Testcontainers aplicando `database/*.sql`, `ApiFactory`, Respawn, builders de dados, `FakeClock`)
- [ ] T022 `qa-guardian` lacra (`scripts/qa-seal.sh seal`) e commit `test(api): add architecture and error catalog tests`

### Implementação da fundação
- [ ] T023 [P] `Domain`: exceções tipadas `ClienteInexistenteException`, `ProdutoInexistenteException`, `EstoqueInsuficienteException`, `ChaveReutilizadaException` em `backend/src/GestaoPedidos.Domain/`
- [ ] T024 [P] `Application/Abstractions`: `ICommandHandler<,>`, `IQueryHandler<,>`, `IClock`, `IUnitOfWork` em `backend/src/GestaoPedidos.Application/Abstractions/`
- [ ] T025 `Api/Errors`: `ErrorCatalog`, `ProblemDetailsFactory` (campos `type`, `title`, `status`, `detail`, `code`, `traceId`, `meta`, `errors`) e `GlobalExceptionHandler` (`IExceptionHandler`) + `StatusCodePages` para 404/415 em `backend/src/GestaoPedidos.Api/Errors/`
- [ ] T026 `Api/Program.cs`: composition root, OpenAPI nativo + Scalar em `/scalar`, health check do SQL em `/health`, logging com `traceId`
- [ ] T027 `Infrastructure`: `GestaoPedidosDbContext` mapeando o schema dos scripts (sem migrations), `UnitOfWork` com execution strategy (`EnableRetryOnFailure`), `SystemClock` em `backend/src/GestaoPedidos.Infrastructure/`
- [ ] T028 [P] `Infrastructure/Resilience`: pipeline `Microsoft.Extensions.Resilience` (retry com backoff só para erros transitórios do SQL) para as leituras Dapper
- [ ] T029 [P] `Application/Abstractions/Decorators`: `ValidationDecorator` (FluentValidation) e `LoggingDecorator` registrados no DI (padrão Decorator)

**Ponto de verificação**: solução compila com `-warnaserror`, testes de arquitetura e do catálogo passam, `/health` responde 200 no compose.

## Fase 3: US1 — Criar pedido com itens (P1) 🎯 MVP

**Objetivo**: criar pedido válido com estoque consistente e erros específicos · **Teste independente**: `POST /pedidos` válido cria; cada regra violada devolve o `code` esperado e nada muda

### Testes (qa-guardian, RED)
- [ ] T030 [P] [US1] Unit `backend/tests/GestaoPedidos.UnitTests/Pedidos/PedidoTests.cs`: `Pedido.Criar` exige ≥ 1 item, quantidade > 0, calcula `ValorTotal`, congela `PrecoUnitario`, data pelo `IClock`
- [ ] T031 [P] [US1] Unit `.../Pedidos/CriarPedidoValidatorTests.cs`: sem itens, > 50 itens, produto repetido, quantidade ≤ 0, `clienteId` ≤ 0, todos os problemas de uma vez
- [ ] T032 [P] [US1] Unit `.../Pedidos/CriarPedidoHandlerTests.cs` (ports mockados): cliente inexistente, produto inexistente, estoque insuficiente (ordem por `ProdutoId`), sucesso, idempotência (mesma chave e mesmo hash devolve o original; hash diferente lança `ChaveReutilizada`)
- [ ] T033 [P] [US1] Integração `backend/tests/GestaoPedidos.IntegrationTests/Pedidos/CriarPedidoTests.cs`: 201 + `Location` + total + estoque reduzido; um teste por `code` (`cliente_inexistente`, `produto_inexistente`, `estoque_insuficiente`, `chave_reutilizada`, `validacao_falhou` com `errors[]` completos) conferindo `status`, `code`, `type`, `traceId`, `meta`; nada gravado nem baixado quando falha; preço congelado após alterar o produto
- [ ] T034 [P] [US1] Integração `.../Pedidos/ConcorrenciaEstoqueTests.cs`: 50 pedidos simultâneos pelo produto com 1 unidade → exatamente 1 × 201, 49 × 422 `estoque_insuficiente`, estoque final 0; pedidos com vários produtos em ordens diferentes não geram deadlock
- [ ] T035 [P] [US1] Integração `.../Pedidos/IdempotenciaTests.cs`: 10 envios simultâneos da mesma chave → 1 pedido, estoque baixado uma vez, respostas apontando para o mesmo pedido; mesma chave com corpo diferente → 422; chaves diferentes e mesmo conteúdo → 2 pedidos; sem chave funciona
- [ ] T036 [P] [US1] Integração `.../RateLimiting/RateLimitTests.cs`: rajada de `POST /pedidos` → 429 `limite_requisicoes_excedido` com `Retry-After` e `meta.retryAfterSegundos`; limite configurável por opções (desligado nos demais testes)
- [ ] T037 [P] [US1] Integração `.../Errors/ErroPadraoTests.cs`: JSON malformado → 400 `corpo_invalido`; `Content-Type` inválido → 415 `formato_nao_suportado`; rota inexistente → 404 `recurso_nao_encontrado`; falha inesperada → 500 `erro_interno` sem vazar detalhe, com `traceId`; banco indisponível → 503 `banco_indisponivel`
- [ ] T038 [US1] `qa-guardian` lacra e commit `test(api): add failing tests for order creation`

### Implementação
- [ ] T039 [P] [US1] `Domain/Pedidos`: `Pedido` (fábrica `Pedido.Criar`) e `ItemPedido`; `Cliente` e `Produto` em `backend/src/GestaoPedidos.Domain/`
- [ ] T040 [P] [US1] `Application/Pedidos/Commands/CriarPedido/`: `CriarPedidoCommand`, `CriarPedidoValidator` (≥ 1 e ≤ 50 itens, quantidade > 0, sem produto repetido), `CriarPedidoResponse`, ports `IPedidoRepository`, `IClienteRepository`, `IProdutoRepository`, `IIdempotenciaRepository`
- [ ] T041 [US1] `Application/Pedidos/Commands/CriarPedido/CriarPedidoHandler.cs`: fluxo do plano (chave → cliente → produtos → baixa atômica em ordem de `ProdutoId` → `Pedido.Criar` → persistir com a chave na mesma transação)
- [ ] T042 [US1] `Infrastructure/Persistence`: repositórios EF; baixa atômica `UPDATE Produtos SET Estoque = Estoque - @q WHERE Id = @id AND Estoque >= @q` (0 linhas → `EstoqueInsuficienteException`); tratamento da violação de PK (2627) da chave de idempotência devolvendo o pedido da vencedora
- [ ] T043 [US1] `Api/Endpoints/PedidosEndpoints.cs`: `POST /pedidos` (`Idempotency-Key` opcional, 201 + `Location`, 200 no reenvio), `MapGroup`, `TypedResults`, política de rate limiting nomeada (token bucket) + limite global por IP em `Api/RateLimiting/`
- [ ] T044 [US1] Registrar handlers, decorators, resiliência e rate limiting no DI; rodar `dotnet test backend` até verde

**Ponto de verificação**: US1 funcional isolada (MVP) e todos os testes da US1 verdes.

## Fase 4: US2 — Listar pedidos (P1)

**Objetivo**: listagem filtrada e paginada · **Teste independente**: seed + filtros por cliente, período e ambos, páginas

### Testes (qa-guardian, RED)
- [ ] T045 [P] [US2] Unit `.../Pedidos/ListarPedidosValidatorTests.cs`: `page` < 1, `pageSize` fora de 1–100, `dataInicio` > `dataFim`, data inválida (todos os problemas juntos)
- [ ] T046 [P] [US2] Integração `.../Pedidos/ListarPedidosTests.cs`: ordem `DataPedido DESC, Id DESC`, filtro por cliente, período inclusivo nas duas pontas (mesma data inicial e final = dia inteiro), paginação sem repetir nem omitir, página além da última, totais, 400 com `pagina_invalida`, `tamanho_pagina_invalido`, `periodo_invertido`, `data_invalida`
- [ ] T047 [US2] `qa-guardian` lacra e commit `test(api): add failing tests for order listing`

### Implementação
- [ ] T048 [P] [US2] `Application/Pedidos/Queries/ListarPedidos/` (query, validator, resposta paginada) e port `IPedidoQueries`
- [ ] T049 [US2] `Infrastructure/Queries/PedidoQueries.cs` (Dapper): intervalo `[dataInicio 00:00 UTC, dataFim + 1 dia)`, `OFFSET/FETCH`, `COUNT`, SQL parametrizado e sargável
- [ ] T050 [US2] `GET /pedidos` em `Api/Endpoints/PedidosEndpoints.cs`; `dotnet test backend` verde

## Fase 5: US3 — Top 5 clientes (P2)

**Objetivo**: relatório correto por cliente · **Teste independente**: seed conhecido contra ranking calculado à mão

### Testes (qa-guardian, RED)
- [ ] T051 [P] [US3] Integração `.../Relatorios/TopClientesTests.cs`: exatamente 5 ordenados por valor desc, homônimos separados, desempate por nome, cliente sem pedido no período ausente, menos de 5 clientes, 400 `periodo_obrigatorio` / `periodo_invertido`
- [ ] T052 [US3] `qa-guardian` lacra e commit `test(api): add failing tests for top customers report`

### Implementação
- [ ] T053 [P] [US3] `Application/Relatorios/Queries/TopClientes/` (query, validator, resposta) e port `IRelatorioQueries`
- [ ] T054 [US3] `Infrastructure/Queries/RelatorioQueries.cs` (Dapper): agrupa por `ClienteId`, `INNER JOIN`, intervalo semiaberto, `ORDER BY Total DESC, Nome`, `TOP 5`
- [ ] T055 [US3] `Api/Endpoints/RelatoriosEndpoints.cs`: `GET /relatorios/top-clientes`

## Fase 6: US5 — Consultas de apoio e detalhe (P3)

### Testes (qa-guardian, RED)
- [ ] T056 [P] [US5] Integração `.../Apoio/ClientesProdutosTests.cs`: `GET /clientes` em ordem alfabética, `GET /produtos` com `sku`, `preco`, `estoque`
- [ ] T057 [P] [US5] Integração `.../Pedidos/ObterPedidoTests.cs`: detalhe com itens e subtotais; 404 `pedido_nao_encontrado` com `meta.pedidoId`
- [ ] T058 [US5] `qa-guardian` lacra e commit `test(api): add failing tests for lookups and order detail`

### Implementação
- [ ] T059 [P] [US5] Queries `Clientes` e `Produtos` e `ObterPedido` em `Application/` + `Infrastructure/Queries/` (Dapper)
- [ ] T060 [US5] `Api/Endpoints/ClientesEndpoints.cs`, `ProdutosEndpoints.cs` e `GET /pedidos/{id}`

## Fase 7: Contrato e CI do backend

- [ ] T061 [P] Teste de contrato `backend/tests/GestaoPedidos.IntegrationTests/Contrato/OpenApiContractTests.cs` (qa-guardian): o documento OpenAPI gerado pela API contém todos os caminhos, métodos, status e `code` de `specs/001-gestao-pedidos/contracts/openapi.yaml`
- [ ] T062 [P] Job `backend` já existe em `.github/workflows/pr-checks.yml`: conferir que roda format, build `-warnaserror`, unit, arquitetura e integração (Testcontainers) e ajustar se preciso
- [ ] T063 Serviço `api` em `docker-compose.yml` (porta 5080, `depends_on: db-init` concluído) e `.env.example` atualizado; `docker compose up --build` sobe banco + API

## Gates do PR 2

- [ ] T064 `qa-guardian`: validação com `scripts/qa-seal.sh check` e veredito APROVADO (conferir cada `code` do catálogo coberto)
- [ ] T065 `scripts/check-guardrails.sh --diff origin/main` e `scripts/check-docs-drift.sh`
- [ ] T066 `code-reviewer` sobre o diff final, sem Bloqueante
- [ ] T067 `/retro`: `docs/domain.md`, `docs/patterns/` com `arquivo:linha`, `docs/playbooks/claude-log.md`, memória
- [ ] T068 `git push`, PR `feat(db,api,docker,ci): ...`, `ci-ok` e `governance` verdes, merge por merge commit

---

# PR 3 — Frontend e e2e (branch `feat/002-frontend-pedidos`)

## Fase 8: Preparação

- [ ] T069 Criar `frontend/` com Vite + React + TypeScript strict, Tailwind e shadcn/ui; scripts `lint`, `typecheck`, `test`, `build` em `frontend/package.json`
- [ ] T070 [P] Estrutura `frontend/src/{app,features/pedidos,features/relatorios,shared}` e `frontend/vitest.config.ts` com Testing Library
- [ ] T071 [P] `frontend/Dockerfile` (build → nginx), `frontend/nginx.conf` (serve o build e faz proxy de `/api` para `api:5080`), `frontend/.dockerignore`; serviço `web` (porta 8080) em `docker-compose.yml`
- [ ] T072 Tipos do contrato em `frontend/src/shared/api/types.ts` e schemas zod espelhando `specs/001-gestao-pedidos/contracts/openapi.yaml`

## Fase 9: Fundação de erros (bloqueia as telas)

### Testes (qa-guardian, RED)
- [ ] T073 [P] Vitest `frontend/src/shared/api/http-client.test.ts`: resposta ProblemDetails vira `ApiError` com `status`, `code`, `meta`, `errors`, `traceId`; falha de rede vira `sem_conexao`; corpo que não é ProblemDetails vira `resposta_invalida`
- [ ] T074 [P] Vitest `frontend/src/shared/api/error-messages.test.ts`: **um teste por `code` do catálogo** (`docs/policies/errors.md`) conferindo a mensagem pt-BR específica com interpolação de `meta` (ex.: produto, solicitado, disponível); código desconhecido cai na mensagem do `status`; nunca texto técnico cru
- [ ] T075 [P] Vitest `frontend/src/shared/components/ui/ErrorAlert.test.tsx` e `FieldError.test.tsx`: exibe mensagem, `traceId` como código de suporte nos erros inesperados, ação "Tentar de novo", espera do `Retry-After` no 429
- [ ] T076 `qa-guardian` lacra e commit `test(web): add failing tests for the error handling layer`

### Implementação
- [ ] T077 [P] `frontend/src/shared/api/http-client.ts` e `ApiError.ts`
- [ ] T078 [P] `frontend/src/shared/api/error-messages.ts` (dicionário `code → mensagem`)
- [ ] T079 [P] `ErrorAlert` e `FieldError` em `frontend/src/shared/components/ui/`

## Fase 10: US2 — Tela de listagem (P1)

### Testes (qa-guardian, RED)
- [ ] T080 [P] [US2] Vitest `frontend/src/features/pedidos/pages/PedidosPage.test.tsx`: lista, filtros por cliente e período refletidos na URL, paginação, estados vazio (com "limpar filtros"), carregando e erro
- [ ] T081 [US2] `qa-guardian` lacra e commit `test(web): add failing tests for the orders list page`

### Implementação
- [ ] T082 [P] [US2] `features/pedidos/api/pedidos.ts`, `schemas/`, `hooks/usePedidos.ts` (TanStack Query com filtros na chave)
- [ ] T083 [US2] `features/pedidos/components/PedidosTable.tsx`, `PedidosFilters.tsx`, `pages/PedidosPage.tsx`; moeda em R$ e datas no fuso local

## Fase 11: US4 + US5 — Criar pedido pela tela (P2)

### Testes (qa-guardian, RED)
- [ ] T084 [P] [US4] Vitest `.../components/NovoPedidoDialog.test.tsx`: escolher cliente, adicionar itens, subtotal e total, estoque visível, validação zod com mensagem junto do campo, produto repetido impedido, botão desabilitado enquanto envia, `Idempotency-Key` novo a cada tentativa de envio, 422 `estoque_insuficiente` mantém o diálogo aberto e mostra produto e quantidade disponível, erro de campo 400 no campo certo, sucesso fecha e invalida a listagem
- [ ] T085 [US4] `qa-guardian` lacra e commit `test(web): add failing tests for the new order dialog`

### Implementação
- [ ] T086 [P] [US5] `features/pedidos/api/` (clientes e produtos) e hooks `useClientes`, `useProdutos`
- [ ] T087 [US4] `features/pedidos/schemas/novo-pedido.ts` (zod: ≥ 1 e ≤ 50 itens, quantidade inteira > 0, sem produto repetido)
- [ ] T088 [US4] `features/pedidos/components/NovoPedidoDialog.tsx` e `hooks/useCriarPedido.ts` (react-hook-form, mutation com `Idempotency-Key`, mapeamento de `errors[].campo` para campos)
- [ ] T089 [P] [US3] Tela simples do relatório `features/relatorios/pages/TopClientesPage.tsx` + hook e teste Vitest correspondente (qa-guardian)
- [ ] T090 `ux-guardian` antes e depois (tokens, estados, acessibilidade, erros por `code`)

## Fase 12: e2e e CI

- [ ] T091 [US4] Playwright (qa-guardian) `frontend/e2e/criar-pedido.spec.ts`: criar pedido pelo diálogo e vê-lo no topo da listagem; `frontend/e2e/estoque-insuficiente.spec.ts`: mensagem específica sem fechar o diálogo; `frontend/playwright.config.ts` contra o compose
- [ ] T092 [P] Conferir/ajustar jobs `frontend` e `e2e` em `.github/workflows/pr-checks.yml` (compose `--wait`, relatório do Playwright como artefato)
- [ ] T093 `docker compose up --build` sobe banco, API e web; fluxo completo em http://localhost:8080

## Gates do PR 3

- [ ] T094 `qa-guardian` APROVADO (`qa-seal check`, todos os `code` com mensagem e teste), `ux-guardian` APROVADO
- [ ] T095 `check-guardrails`, `check-docs-drift`, `code-reviewer` sem Bloqueante, `/retro`
- [ ] T096 PR `feat(web,e2e,docker,ci): ...`, `ci-ok` e `governance` verdes, merge por merge commit

---

# PR 4 — Query, CD e entrega (branch `perf/003-query-ranking-e-entrega`)

## Fase 13: Parte 4 — debug da query (com `sql-tuner` e `/sql-investigate`)

- [ ] T097 Salvar a query original em `database/04-query-debug-original.sql`; medir com `STATISTICS IO, TIME` e plano real via MCP (sem credenciais no repositório)
- [ ] T098 [P] Testes de integração (qa-guardian) `.../Relatorios/QueryCorrigidaTests.cs`: a query corrigida bate com o ranking esperado (homônimos, ordem, ano de 2026)
- [ ] T099 Escrever `database/05-query-debug-fixed.sql` (sem `YEAR()` na coluna, `INNER JOIN`, agrupar por `c.Id, c.Nome`, `ORDER BY Total DESC`, `TOP`) e confirmar/remover índices com evidência em `database/02-schema.sql`
- [ ] T100 Registrar antes/depois (leituras lógicas, operadores, tempo) em `docs/playbooks/query-debug.md`

## Fase 14: CD, README e vídeo

- [ ] T101 `.github/workflows/cd.yml`: push na `main` → build e publicação das imagens `api` e `web` no GHCR (tags `sha` e `latest`), sem deploy
- [ ] T102 README final: como rodar com Docker, como rodar os testes, decisões técnicas (links dos ADRs), padrão de erros, documentação da API (Scalar + `contracts/openapi.yaml`), uso do MCP, Parte 4, CI/CD, "Como usei o Claude" (a partir de `docs/playbooks/claude-log.md`), próximos passos
- [ ] T103 `docs/playbooks/video.md`: roteiro de até 10 minutos
- [ ] T104 Gates do PR 4: `qa-guardian`, `check-guardrails`, `check-docs-drift`, `code-reviewer`, `/retro`, PR `perf(db,docs,ci): ...`, merge e conferência final do `docker compose up` do zero

---

## Dependências e ordem

- PR 1 → PR 2 → PR 3 → PR 4 (cada um parte da `main` atualizada).
- Dentro do PR 2: Preparação → Fundação → US1 (MVP) → US2 → US3 → US5 → Contrato. US2, US3 e US5 dependem só da Fundação e podem ser feitas em qualquer ordem depois dela.
- Dentro do PR 3: Preparação → Fundação de erros → US2 (tela) → US4/US5 → e2e.
- Em cada história: testes RED (qa-guardian) → commit `test` → implementação → commit `feat` → verde.

## Oportunidades de paralelismo

- Preparação: T009, T010, T013, T014, T015 em paralelo.
- Testes de uma mesma história (marcados [P]) escritos juntos pelo `qa-guardian`.
- Domínio e Application de uma história (T039/T040), e os hooks/api do front (T082, T086).

## Estratégia

MVP = PR 2 até a US1 (criar pedido consistente, erros específicos, idempotência, concorrência). Depois entrega incremental por história. Commits pequenos e atômicos; push só no fim de cada PR ou em marcos.

## Contagem

104 tarefas: PR 1 = 8, PR 2 = 60, PR 3 = 28, PR 4 = 8.
