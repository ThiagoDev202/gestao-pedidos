---
name: new-endpoint
description: Guia a criação de um endpoint novo no backend .NET do Gestão de Pedidos seguindo o padrão do projeto (Minimal API + Command/Query + Handler + Validator + port/adapter + testes + contrato + cliente do front). Use quando a feature pedir uma rota nova ("preciso de um endpoint para X"), depois que a spec estiver aprovada e antes de escrever o primeiro arquivo.
---

# /new-endpoint — endpoint novo no padrão do projeto

Pré-requisitos: spec aprovada (`/speckit-specify` → `/speckit-plan`), `impact-analyzer` quando tocar schema ou o front, e o endpoint descrito em `specs/<NNN>/contracts/openapi.yaml`. A skill não gera arquivos sozinha: conduz o passo a passo e cobra o checklist.

## 1. Decidir Command ou Query (CQRS leve)
- **Command** (altera estado): passa pelo agregado de domínio, `IUnitOfWork` e EF Core. Ex.: `POST /pedidos`.
- **Query** (só lê): SQL explícito via Dapper, devolve DTO, sem passar pelo agregado. Ex.: `GET /pedidos`, `GET /relatorios/top-clientes`.

## 2. Testes primeiro
Acione o `qa-guardian` (RED): unitário do handler (ports mockados) e do validator, teste de integração do endpoint (status, corpo, ProblemDetails, efeitos no banco) e, se mexer no schema, o cenário de concorrência. Só depois implemente; não edite os testes.

## 3. Estrutura (referência: o caso de uso `CriarPedido`)
```
backend/src/GestaoPedidos.Domain/<Agregado>/…                       regras e invariantes (só Command)
backend/src/GestaoPedidos.Application/<Agregado>/{Commands|Queries}/<Caso>/
  <Caso>Command.cs | <Caso>Query.cs      request imutável (record)
  <Caso>Handler.cs                       ICommandHandler<,> ou IQueryHandler<,>
  <Caso>Validator.cs                     FluentValidation (validação estrutural → 400)
  <Caso>Response.cs                      DTO de saída
backend/src/GestaoPedidos.Application/<Agregado>/Ports/…             interface do repositório ou da query
backend/src/GestaoPedidos.Infrastructure/<Agregado>/…                adapter (EF ou Dapper) e mapeamento
backend/src/GestaoPedidos.Api/Endpoints/<Recurso>Endpoints.cs        MapGroup + TypedResults
```
- **Endpoint**: só HTTP. Lê o request, chama o handler, mapeia o resultado para `TypedResults` (201 + `Location`, 200, 404, 422). Sem regra de negócio, sem EF, sem SQL.
- **Handler**: orquestra; regra de negócio no domínio. Exceção de domínio tipada → ProblemDetails 422 no `IExceptionHandler`.
- **Validator**: formato e limites; registrado pelo Decorator de validação no DI (não chame manualmente no handler).
- **Escrita**: baixa de estoque atômica no banco; transação via `IUnitOfWork` com a execution strategy do EF (retry de transientes).
- **Leitura**: Dapper com SQL parametrizado e sargável, paginação com `OFFSET/FETCH` e `ORDER BY` determinístico.
- **Resiliência**: rate limiting declarado por política nomeada no `MapGroup`/endpoint; sem circuit breaker (ADR-007).
- **Código novo**: `Nullable`, sem comentários, nomes de domínio em pt-BR e sufixos técnicos em inglês.

## 4. Registro e documentação
- Registre o handler em `Application/DependencyInjection.cs` (os decorators de validação e log já envolvem todo handler registrado); mapeie o grupo em `Api/Composition/WebApplicationExtensions.cs` (`UseGestaoPedidos`).
- Atualize `contracts/openapi.yaml` e confira que o OpenAPI gerado (Scalar) bate com ele.
- Schema novo: `database/*.sql` idempotente + mapeamento EF; índice com justificativa de plano de execução (`/sql-investigate`).

## 5. Consumidores
Cliente em `frontend/src/features/<feature>/api/` e schema zod/tipos correspondentes (`/new-screen` quando houver tela).

## 6. Fechamento
`qa-guardian` (validação), `code-reviewer` no diff final, gates do backend e `/ship`; encerre com `/retro`.
