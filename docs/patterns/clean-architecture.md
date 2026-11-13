# Clean Architecture neste projeto

```
Api ──► Application ──► Domain
Infrastructure ──► Application ──► Domain
```

| Projeto | Contém | Referencia |
|---|---|---|
| `GestaoPedidos.Domain` | `Pedido` (agregado, fábrica `Pedido.Criar`: `backend/src/GestaoPedidos.Domain/Pedidos/Pedido.cs:23`), `ItemPedido`, `Cliente`, `Produto`, exceções de domínio (`Excecoes/`) | nada |
| `GestaoPedidos.Application` | casos de uso (`Pedidos/Commands/CriarPedido/`, `Pedidos/Queries/`, `Relatorios/Queries/`), validators, ports (`Pedidos/Ports/`, `Relatorios/Ports/`, `Apoio/Ports/`), `ICommandHandler`/`IQueryHandler`, decorators de validação | `Domain` |
| `GestaoPedidos.Infrastructure` | `GestaoPedidosDbContext`, repositórios EF, `UnitOfWork`, consultas Dapper (`Queries/`), pipeline de retry (`Resilience/ConsultaSql.cs`), decorators de log, `SystemClock` | `Application` |
| `GestaoPedidos.Api` | Minimal APIs (`Endpoints/`), catálogo de erros (`Errors/ErrorCatalog.cs`), rate limiting (`RateLimiting/`), composição (`Composition/`) | `Application`, `Infrastructure` (só em `Composition`) |

## Regra de dependência (imposta por teste)
`backend/tests/GestaoPedidos.ArchitectureTests/DependencyRulesTests.cs`: `Domain` só depende de `System`; `Application` só de `Domain`, FluentValidation e `Microsoft.Extensions.DependencyInjection`; `Infrastructure` não depende de `Api`; tipos de `Api` fora de `GestaoPedidos.Api.Composition` não usam EF Core nem Dapper.

## CQRS leve
- **Escrita**: `CriarPedidoHandler` (`Application/Pedidos/Commands/CriarPedido/CriarPedidoHandler.cs`) roda dentro de `IUnitOfWork.ExecuteAsync` (`Infrastructure/Persistence/UnitOfWork.cs:9`: execution strategy do EF + transação). A baixa de estoque é um `UPDATE ... WHERE Estoque >= @q` atômico (`Infrastructure/Persistence/ProdutoRepository.cs:15`), em ordem crescente de `ProdutoId`; a idempotência serializa pela chave com `sp_getapplock` (`Infrastructure/Persistence/IdempotenciaRepository.cs:12`).
- **Leitura**: `PedidoQueries`, `RelatorioQueries` e `ApoioQueries` (Dapper, SQL parametrizado, intervalo semiaberto de datas) em `Infrastructure/Queries/`.
- **Decorators** registrados no DI: validação (`Application/DependencyInjection.cs:23`) e log (`Infrastructure/DependencyInjection.cs:61`); o log é a camada mais externa.

## Fluxo de um erro
Exceção de domínio ou `RequisicaoInvalidaException` → `GlobalExceptionHandler` → `ErrorCatalog.Mapear` → `ProblemaWriter` (ProblemDetails com `code`, `traceId`, `meta`, `errors`). Ver `docs/policies/errors.md`.
