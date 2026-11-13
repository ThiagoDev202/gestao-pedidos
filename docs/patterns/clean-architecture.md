# Clean Architecture neste projeto

```
Api ──► Application ──► Domain
Infrastructure ──► Application ──► Domain
```

| Projeto | Contém | Pode referenciar |
|---|---|---|
| `GestaoPedidos.Domain` | entidades, agregado `Pedido`, `ItemPedido`, exceções de domínio | nada |
| `GestaoPedidos.Application` | Commands/Queries, handlers, validators, ports (`IPedidoRepository`, `IPedidoQueries`, `IUnitOfWork`, `IClock`) | `Domain` |
| `GestaoPedidos.Infrastructure` | `DbContext` EF, repositórios, queries Dapper, `UnitOfWork`, relógio, resiliência | `Application` |
| `GestaoPedidos.Api` | Minimal APIs, DI, ProblemDetails, OpenAPI/Scalar, rate limiting | `Application`, `Infrastructure` (só no composition root) |

A regra é imposta por `GestaoPedidos.ArchitectureTests` (NetArchTest): `Domain` sem dependências; `Application` só em `Domain`; `Api` não usa `Microsoft.EntityFrameworkCore` nem `Dapper` fora do composition root.

Estado atual: projetos ainda não criados (previsto no PR de backend). Este doc passa a citar `arquivo:linha` quando existirem.
