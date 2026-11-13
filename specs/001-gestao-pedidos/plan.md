# Plano de Implementação: Mini Gestão de Pedidos

**Branch**: `docs/001-sdd-gestao-pedidos` | **Data**: 2026-11-13 | **Spec**: [spec.md](spec.md)

**Entrada**: especificação em `specs/001-gestao-pedidos/spec.md`

## Resumo

Criar e consultar pedidos de clientes com estoque consistente. Abordagem: API .NET 10 em Clean Architecture com CQRS leve (escrita por agregado + EF Core em transação com baixa de estoque atômica no banco; leitura por SQL Dapper afinado), banco SQL Server versionado em `database/*.sql`, frontend React/TypeScript e tudo em `docker compose up`. Decisões e alternativas em [research.md](research.md); contrato em [contracts/openapi.yaml](contracts/openapi.yaml); modelo em [data-model.md](data-model.md); validação ponta a ponta em [quickstart.md](quickstart.md).

## Contexto Técnico

**Linguagem/Versão**: C# / .NET 10 LTS (backend); TypeScript 5 strict, Node 22 (frontend)
**Dependências principais**: ASP.NET Core Minimal APIs, EF Core (SqlServer), Dapper, FluentValidation, Microsoft.Extensions.Resilience (Polly), Scalar.AspNetCore; React, Vite, Tailwind, shadcn/ui, TanStack Query, react-hook-form, zod
**Armazenamento**: SQL Server 2022 em container; schema por scripts SQL idempotentes (sem migrations do EF)
**Testes**: xUnit + Shouldly + NSubstitute (unit), NetArchTest (arquitetura), WebApplicationFactory + Testcontainers.MsSql + Respawn (integração), Vitest + Testing Library (front), Playwright (e2e)
**Plataforma alvo**: containers Linux via Docker Compose; navegadores modernos
**Tipo de projeto**: aplicação web (backend + frontend + banco)
**Metas de desempenho**: primeira página da listagem e relatório de top clientes < 1 s com ~50 mil pedidos (SC-004); 50 pedidos concorrentes pelo último item → exatamente 1 aceito (SC-003)
**Restrições**: baixa de estoque atômica; transação tudo-ou-nada; rate limiting no `POST /pedidos`; sem circuit breaker (ADR-007); sem bibliotecas de licença comercial
**Escala/Escopo**: ~200 clientes, ~100 produtos, ~50 mil pedidos no seed; 7 operações (6 de negócio + `/health`); 1 tela (listagem com diálogo de criação). O relatório de top clientes fica só na API (Princípio VII: a spec não pede tela)
**Código existente**: nenhum (projeto greenfield); `impact-analyzer` não tem o que mapear

## Constitution Check

*GATE: passa antes da Fase 0 e foi reavaliado depois da Fase 1.*

| Princípio | Como o plano atende | Status |
|---|---|---|
| I. Código é a fonte da verdade | Sem código atual a citar; docs de arquitetura passam a citar `arquivo:linha` quando o código existir; `/retro` e `docs-drift` conferem | OK |
| II. SDD e aprovação humana | spec → clarify (2 decisões do usuário) → plan → tasks → analyze; pausas após spec e após plano + tarefas; commits e push só sob pedido | OK |
| III. Contrato explícito | `contracts/openapi.yaml` é a fonte; front tipa por schemas zod espelhando o contrato; erros em ProblemDetails com 400/422/404 | OK |
| IV. Clean Architecture verificada | 4 projetos + `ArchitectureTests`; CQRS leve (EF na escrita, Dapper na leitura); regra em Domain/Application | OK |
| V. Teste antes do código | `qa-guardian` RED por caso de uso, lacre `qa-seal`; unit + arquitetura + integração (Testcontainers) + Vitest + Playwright | OK |
| VI. Banco por scripts e medido | `database/*.sql` idempotentes executados pelo `db-init`; EF sem migrations; cada índice justificado por plano de execução (PR 3) | OK |
| VII. Simplicidade | GoF só os 5 nomeados e justificados em `docs/patterns/solid-gof.md`; sem MediatR, sem read DB separado, sem circuit breaker | OK |

Sem violações: a tabela de Complexidade fica vazia. Reavaliação pós-design: o desenho de dados e o contrato não introduziram dependência nem camada nova.

## Estrutura do Projeto

### Documentação (esta feature)

```text
specs/001-gestao-pedidos/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/openapi.yaml
├── checklists/requirements.md
└── tasks.md            (gerado por /speckit-tasks)
```

### Código-fonte (raiz do repositório)

```text
database/
├── 01-create-database.sql
├── 02-schema.sql
├── 03-seed.sql
├── 04-query-debug-original.sql
├── 05-query-debug-fixed.sql
└── init/run-scripts.sh

backend/
├── GestaoPedidos.slnx  Directory.Build.props  Directory.Packages.props  Dockerfile  .dockerignore
├── src/
│   ├── GestaoPedidos.Domain/
│   │   └── Pedidos/ (Pedido, ItemPedido, exceções)  Clientes/  Produtos/
│   ├── GestaoPedidos.Application/
│   │   ├── Abstractions/ (ICommandHandler, IQueryHandler, IClock, IUnitOfWork, decorators)
│   │   ├── Pedidos/Commands/CriarPedido/  Pedidos/Queries/{ListarPedidos,ObterPedido}/
│   │   ├── Relatorios/Queries/TopClientes/  Clientes/Queries/  Produtos/Queries/
│   │   └── Pedidos/Ports/ (IPedidoRepository, IPedidoQueries) …
│   ├── GestaoPedidos.Infrastructure/
│   │   ├── Persistence/ (GestaoPedidosDbContext, mapeamentos, UnitOfWork)
│   │   ├── Queries/ (Dapper: Pedido, Relatorio, Cliente, Produto)
│   │   └── Resilience/  Time/
│   └── GestaoPedidos.Api/
│       ├── Endpoints/ (Pedidos, Relatorios, Clientes, Produtos)
│       ├── Errors/ (ErrorCatalog, ProblemDetailsFactory, IExceptionHandler)  RateLimiting/  Program.cs
└── tests/
    ├── GestaoPedidos.UnitTests/  GestaoPedidos.ArchitectureTests/  GestaoPedidos.IntegrationTests/

frontend/
├── Dockerfile  nginx.conf  package.json  vite.config.ts  playwright.config.ts
├── src/
│   ├── app/  features/pedidos/{api,components,hooks,schemas,pages}
│   └── shared/{api (http-client, ApiError, error-messages),components/ui (ErrorAlert, FieldError),lib}
└── e2e/

docker-compose.yml
```

**Decisão de estrutura**: aplicação web com backend e frontend separados no mesmo repositório (sem código compartilhado), banco em `database/` e orquestração na raiz; é a "Opção 2" do template, ampliada pela Clean Architecture.

## Fluxos críticos

**Criar pedido** (`CriarPedidoHandler`, dentro de `strategy.ExecuteAsync` + transação):
0. Abrir a transação e, havendo `Idempotency-Key`, tomar `sp_getapplock` (`Exclusive`, `LockOwner = Transaction`) com a chave: requisições simultâneas com a mesma chave são serializadas **antes** de tocar o estoque (sem isso a segunda ficaria presa no `UPDATE` e devolveria 422 em vez do pedido da primeira).
1. Validator (400): ≥ 1 e ≤ 50 itens, quantidades > 0, sem produto repetido.
1b. Com o lock da chave: chave já registrada com o mesmo hash do corpo → devolve o pedido original (200); hash diferente → `ChaveReutilizada` (422).
2. Cliente existe? senão `ClienteInexistente` → 422.
3. Carrega produtos por id; faltando algum → `ProdutoInexistente` → 422.
4. Para cada item **ordenado por `ProdutoId`**: `UPDATE Produtos SET Estoque = Estoque - @q WHERE Id = @id AND Estoque >= @q`; 0 linhas → `EstoqueInsuficiente` (rollback) → 422. A ordem fixa evita deadlock entre pedidos concorrentes.
5. `Pedido.Criar(cliente, itens com preço congelado, clock)` calcula o total; persistir pedido, itens e o registro de idempotência (PK = chave) na **mesma transação**; commit. O `sp_getapplock` faz a segunda requisição esperar a primeira e então encontrar o registro dela (devolve o pedido original); a PK da chave é a última defesa.

**Listar/relatório** (Dapper): intervalo `[dataInicio 00:00 UTC, dataFim + 1 dia)`; `OFFSET/FETCH` com `ORDER BY DataPedido DESC, Id DESC`; top 5 agrupado por `ClienteId`, `ORDER BY Total DESC, Nome`.

## Fatias de entrega (PRs)

| PR | Branch | Conteúdo |
|---|---|---|
| 1 (atual) | `docs/001-sdd-gestao-pedidos` | spec, clarify, plano, contrato, tarefas, `docs/spec.md`, `docs/plan.md`; padrão de erros e ADR-009; correção do `.mcp.json` e do guardrail de credenciais; enxugamento do Spec Kit e templates em pt-BR; pendências como issues do GitHub; ajustes do `pr-checks.yml` |
| 2 | `feat/001-backend-pedidos` | banco + compose (sqlserver, db-init, api), solução .NET, POST/GET pedidos, relatório, apoio, resiliência, testes unit/arquitetura/integração, job `backend` no CI |
| 3 | `feat/002-frontend-pedidos` | frontend (listagem, diálogo de criação, camada de erros), nginx, serviço `web` no compose, Vitest, Playwright, jobs `frontend` e `e2e` |
| 4 | `perf/003-query-ranking-e-entrega` | Parte 4 (investigação com `sql-tuner`, índices), CD (GHCR), README final, roteiro do vídeo |

## Acompanhamento de Complexidade

Sem violações da constitution; nada a justificar.
