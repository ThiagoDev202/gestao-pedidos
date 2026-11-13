# SOLID e padrões GoF: onde aparecem e por quê

## SOLID
| Princípio | Onde |
|---|---|
| SRP | um handler por caso de uso; endpoint só HTTP; validator só formato |
| OCP | novo caso de uso = nova pasta + registro no DI; decorators acrescentam comportamento sem mexer em handlers |
| LSP | repositórios e queries substituíveis por fakes nos testes unitários |
| ISP | ports pequenos por agregado; leitura (`IPedidoQueries`) separada de escrita (`IPedidoRepository`) |
| DIP | `Application` define ports; `Infrastructure` implementa; o `Domain` não conhece ninguém |

## GoF (só onde resolve problema real)
| Padrão | Onde | Problema que resolve |
|---|---|---|
| Command | `CriarPedidoCommand` + `CriarPedidoHandler` | representa a intenção como objeto, uniformiza entrada dos casos de uso |
| Decorator | validação e log em volta dos handlers (DI) | preocupações transversais sem poluir os handlers |
| Factory Method | `Pedido.Criar(...)` | garante invariantes na criação (≥ 1 item, quantidade > 0, total calculado) |
| Adapter | repositórios e queries sobre EF/Dapper | adapta tecnologia aos ports da `Application` |
| Builder | builders de dados nos testes | cenários legíveis nos testes do `qa-guardian` |

Sem MediatR, Strategy "de enfeite" ou Singleton manual. Padrão novo só com problema nomeado no plano (Princípio VII).
