# Modelo de Dados: Mini Gestão de Pedidos

Fonte do schema: `database/02-schema.sql` (idempotente). O EF Core apenas mapeia.

```text
Clientes 1 ──< Pedidos 1 ──< ItensPedido >── 1 Produtos
                  Pedidos 1 ──0..1 IdempotenciaPedidos
```

## Clientes
| Coluna | Tipo | Regra |
|---|---|---|
| Id | int identity | PK |
| Nome | nvarchar(200) | obrigatório (não único: há homônimos) |
| Email | nvarchar(254) | obrigatório, `UNIQUE` |
| CriadoEm | datetime2 | default UTC |

## Produtos
| Coluna | Tipo | Regra |
|---|---|---|
| Id | int identity | PK |
| Nome | nvarchar(200) | obrigatório |
| Sku | nvarchar(50) | obrigatório, `UNIQUE` |
| Preco | decimal(18,2) | `CHECK (Preco >= 0)` |
| Estoque | int | `CHECK (Estoque >= 0)` |

## Pedidos
| Coluna | Tipo | Regra |
|---|---|---|
| Id | int identity | PK |
| ClienteId | int | FK → Clientes.Id |
| DataPedido | datetime2(3) | UTC, definida pelo servidor |
| ValorTotal | decimal(18,2) | soma de `Quantidade × PrecoUnitario`, `CHECK (ValorTotal >= 0)` |

## ItensPedido
| Coluna | Tipo | Regra |
|---|---|---|
| Id | int identity | PK |
| PedidoId | int | FK → Pedidos.Id, `ON DELETE CASCADE` |
| ProdutoId | int | FK → Produtos.Id |
| Quantidade | int | `CHECK (Quantidade > 0)` |
| PrecoUnitario | decimal(18,2) | congelado na criação, `CHECK (PrecoUnitario >= 0)` |
| — | — | `UNIQUE (PedidoId, ProdutoId)` |

## IdempotenciaPedidos
| Coluna | Tipo | Regra |
|---|---|---|
| Chave | uniqueidentifier | PK (valor do header `Idempotency-Key`) |
| HashCorpo | char(64) | SHA-256 do corpo normalizado (cliente + itens ordenados) |
| PedidoId | int | FK → Pedidos.Id, `ON DELETE CASCADE` |
| CriadoEm | datetime2(3) | UTC |

## Índices (a confirmar por plano de execução)
| Índice | Colunas | INCLUDE | Serve a |
|---|---|---|---|
| IX_Pedidos_ClienteId_DataPedido | ClienteId, DataPedido | ValorTotal | listagem por cliente e período |
| IX_Pedidos_DataPedido | DataPedido | ClienteId, ValorTotal | listagem por período, top clientes |
| IX_ItensPedido_PedidoId | PedidoId | ProdutoId, Quantidade, PrecoUnitario | detalhe do pedido, relatório original (Parte 4) |
| IX_ItensPedido_ProdutoId | ProdutoId | — | FK de produto |

A restrição `UNIQUE (PedidoId, ProdutoId)` já cria um índice por `PedidoId` à esquerda; o PR 4 decide com plano se `IX_ItensPedido_PedidoId` ainda se justifica.

## Domínio (C#)
- **Pedido** (agregado raiz): `Id`, `ClienteId`, `DataPedido`, `Itens` (somente leitura), `ValorTotal`. Fábrica `Pedido.Criar(clienteId, itens, clock)` garante ≥ 1 item, quantidades > 0 e calcula o total.
- **ItemPedido**: `ProdutoId`, `Quantidade`, `PrecoUnitario`, `Subtotal`.
- **Cliente** e **Produto**: entidades de consulta e escrita de estoque (a baixa é feita no banco).
- Exceções de domínio: `ClienteInexistenteException`, `ProdutoInexistenteException`, `EstoqueInsuficienteException`, `ChaveReutilizadaException` → 422.

## Seed (`03-seed.sql`)
Set-based e determinístico: ~200 clientes (inclui homônimos), ~100 produtos com estoques variados (alguns baixos, um com 1 unidade), ~50 mil pedidos em 2025–2026 com 1 a 5 itens, `ValorTotal` consistente com os itens.
