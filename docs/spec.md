# Especificação: Mini Gestão de Pedidos

Visão consolidada exigida pelo teste técnico. A especificação completa, com cenários Dado/Quando/Então por história, está em [`specs/001-gestao-pedidos/spec.md`](../specs/001-gestao-pedidos/spec.md); o contrato em [`contracts/openapi.yaml`](../specs/001-gestao-pedidos/contracts/openapi.yaml); o modelo em [`data-model.md`](../specs/001-gestao-pedidos/data-model.md); o padrão de erros em [`policies/errors.md`](policies/errors.md).

## 1. Objetivo

Permitir que um **Operador** cadastre pedidos de clientes (com itens e controle de estoque) e os consulte com filtros e paginação, e que um **Gestor** veja os 5 clientes que mais compraram em um período. Roda inteiramente em Docker com um único comando.

## 2. Regras de negócio

1. Pedido exige **cliente existente**; senão, erro de regra de negócio (422).
2. Pedido exige de **1 a 50 itens**, quantidade **maior que zero** e **sem produto repetido** (400, com todos os problemas de uma vez).
3. Cada produto deve existir e ter **estoque suficiente**; senão, 422 identificando o produto e o estoque disponível. **Tudo ou nada**: nada é gravado nem baixado quando qualquer item falha.
4. O **preço unitário** vem do cadastro do produto na criação e fica congelado no item; o cliente da API não envia preço. `ValorTotal = Σ quantidade × preço unitário`, guardado no pedido.
5. `DataPedido` é definida pelo servidor, em **UTC**.
6. **Concorrência**: a baixa de estoque é atômica no banco; dois pedidos pelo último item aceitam exatamente um e o estoque nunca fica negativo.
7. **Idempotência**: o header opcional `Idempotency-Key` faz o reenvio da mesma requisição devolver o pedido original sem novo efeito; a mesma chave com corpo diferente é recusada (422 `chave_reutilizada`).
8. **Listagem**: filtros opcionais por cliente e período (datas inclusivas), `page` ≥ 1, `pageSize` de 1 a 100, ordem `DataPedido` decrescente.
9. **Top clientes**: 5 maiores valores totais no período, agrupados por **cliente** (não por nome), desempate por nome; período obrigatório.
10. **Limite de taxa** no `POST /pedidos` por origem de rede (429 com `Retry-After`).

## 3. Endpoints

| Método e rota | Resposta de sucesso | Erros |
|---|---|---|
| `POST /pedidos` (`Idempotency-Key` opcional) | 201 + `Location` (200 no reenvio idempotente) | 400, 415, 422, 429, 500, 503 |
| `GET /pedidos?clienteId&dataInicio&dataFim&page&pageSize` | 200 `{ items, page, pageSize, totalItems, totalPages }` | 400 |
| `GET /pedidos/{id}` | 200 detalhe com itens | 404 |
| `GET /relatorios/top-clientes?dataInicio&dataFim` | 200 até 5 `{ clienteId, nome, valorTotal, quantidadePedidos }` | 400 |
| `GET /clientes` · `GET /produtos` | 200 listas (produtos com preço e estoque) | — |
| `GET /health` | 200 (503 se o banco cair) | — |

Qualquer rota pode ainda devolver 429, 500 e 503. Todo erro é ProblemDetails com `code` específico, `traceId`, `meta` e `errors[]` (ver [`policies/errors.md`](policies/errors.md)). Documentação interativa em `/scalar`.

## 4. Modelo de dados (SQL Server)

```text
Clientes 1 ──< Pedidos 1 ──< ItensPedido >── 1 Produtos
                  Pedidos 1 ──0..1 IdempotenciaPedidos
```

| Tabela | Colunas principais | Restrições |
|---|---|---|
| Clientes | Id, Nome, Email, CriadoEm | PK; `Email` único |
| Produtos | Id, Nome, Sku, Preco, Estoque | PK; `Sku` único; `Preco ≥ 0`; `Estoque ≥ 0` |
| Pedidos | Id, ClienteId, DataPedido, ValorTotal | PK; FK → Clientes; `ValorTotal ≥ 0` |
| ItensPedido | Id, PedidoId, ProdutoId, Quantidade, PrecoUnitario | PK; FKs; `Quantidade > 0`; único (PedidoId, ProdutoId) |
| IdempotenciaPedidos | Chave, HashCorpo, PedidoId, CriadoEm | PK = chave; FK → Pedidos |

Índices: `(ClienteId, DataPedido) INCLUDE (ValorTotal)`, `(DataPedido) INCLUDE (ClienteId, ValorTotal)`, `ItensPedido(PedidoId) INCLUDE (...)`, `ItensPedido(ProdutoId)`. São provisórios: cada um é confirmado ou removido com plano de execução (Parte 4). Detalhes em [`data-model.md`](../specs/001-gestao-pedidos/data-model.md). Scripts de criação e carga em `database/`, executados automaticamente no `docker compose up`.

## 5. Critérios de aceite (resumo)

| História | Critério verificável |
|---|---|
| US1 Criar pedido (P1) | pedido válido cria com total correto e estoque reduzido; cada regra violada devolve o `code` esperado e **nada muda**; 50 pedidos simultâneos pelo último item → exatamente 1 aceito; 10 envios simultâneos da mesma chave → 1 pedido |
| US2 Listar (P1) | filtros por cliente e período inclusivo, paginação sem repetir nem omitir, parâmetros inválidos → 400 |
| US3 Top clientes (P2) | 5 clientes por valor decrescente, homônimos separados, desempate por nome, resultado igual ao cálculo manual do seed |
| US4 Criar pela tela (P2) | cria, atualiza a listagem, mostra a mensagem específica de cada erro sem fechar o formulário; botão desabilitado ao enviar |
| US5 Apoio (P3) | listas de clientes e produtos e detalhe do pedido; 404 para pedido inexistente |

Metas: primeira página da listagem e relatório em menos de 1 s com ~50 mil pedidos; todo `code` de erro com teste na API e mensagem específica na interface. A lista completa (FR-001 a FR-024, SC-001 a SC-009) está na spec.

## 6. Fora de escopo

Autenticação, edição/cancelamento de pedidos, devolução de estoque, cadastro de clientes e produtos pela interface, tela do relatório de top clientes (só API), expiração de chaves de idempotência.
