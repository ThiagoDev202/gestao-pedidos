# Domínio: Gestão de Pedidos

Glossário e regras de negócio. Hipótese até confirmada no código (Princípio I); quando o backend existir, cada regra cita o arquivo que a implementa.

## Atores
- **Operador**: cria pedidos e consulta a listagem.
- **Gestor**: consulta relatórios (top clientes por período).

## Glossário
| Termo | Significado |
|---|---|
| Cliente | Pessoa ou empresa que compra. Tem nome e e-mail único. |
| Produto | Item vendável com SKU único, preço atual e estoque. |
| Pedido | Compra de um cliente numa data (UTC), com um ou mais itens e valor total. |
| ItemPedido | Linha do pedido: produto, quantidade e preço unitário **congelado** no momento da criação. |
| ValorTotal | Soma de `Quantidade × PrecoUnitario` dos itens, persistida em `Pedidos`. |

## Regras de negócio
1. Pedido exige cliente existente, senão erro de negócio (422).
2. Pedido exige de 1 a 50 itens, cada quantidade > 0 e nenhum produto repetido (400, todos os problemas de uma vez).
3. Cada produto precisa existir (422) e ter estoque ≥ quantidade pedida (422). Se qualquer item falhar, nada é gravado nem baixado.
4. O preço unitário vem de `Produtos.Preco` na criação; o cliente da API não envia preço.
5. Criar o pedido baixa o estoque de forma atômica; pedidos concorrentes pelo último item: só um passa.
6. `DataPedido` é definida pelo servidor em UTC. Filtros `dataInicio`/`dataFim` são datas inclusivas, tratadas como `[dataInicio, dataFim + 1 dia)`.
7. Listagem: paginada (`page` ≥ 1, `pageSize` 1–100), ordenada por `DataPedido` decrescente, filtrável por cliente e período.
8. Top clientes: os 5 clientes de maior valor total de pedidos no período, agrupados por **cliente** (não por nome), desempate por nome.
9. Reenvio com a mesma `Idempotency-Key` e o mesmo corpo devolve o pedido original, sem novo efeito; a mesma chave com corpo diferente é recusada (422 `chave_reutilizada`).
10. Hash de idempotência: SHA-256 hexadecimal minúsculo de `"{ClienteId}|"` + itens ordenados por produto como `produtoId:quantidade` separados por vírgula (`CriarPedidoHandler.CalcularHash`).
11. Todo erro segue o padrão de `docs/policies/errors.md`.
