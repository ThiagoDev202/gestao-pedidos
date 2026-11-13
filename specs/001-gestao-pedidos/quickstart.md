# Quickstart: validação ponta a ponta

Pré-requisito: Docker com Compose. Detalhes dos endpoints em [contracts/openapi.yaml](contracts/openapi.yaml) e do schema em [data-model.md](data-model.md).

## 1. Subir tudo
```bash
cp .env.example .env
docker compose up --build
```
Esperado: `sqlserver` saudável → `db-init` conclui com código 0 (schema e seed aplicados) → `api` em http://localhost:5080 → `web` em http://localhost:8080.

## 2. Saúde e contrato
- `GET http://localhost:5080/health` → 200.
- `http://localhost:5080/scalar` mostra os endpoints do contrato.

## 3. Cenários da spec (via API)
| Cenário | Chamada | Esperado |
|---|---|---|
| US1 feliz | `POST /pedidos` com cliente e itens válidos | 201 + `Location`; total = soma; estoque reduzido (`GET /produtos`) |
| US1 cliente inexistente | `clienteId` 999999 | 422 `cliente_inexistente`; nada gravado |
| US1 sem itens / quantidade 0 / produto repetido / > 50 itens | corpo inválido | 400 com `errors` |
| US1 estoque insuficiente | quantidade acima do estoque | 422 `estoque_insuficiente`; estoque intacto |
| US1 concorrência | 2+ `POST` simultâneos pelo produto com 1 unidade | 1 × 201, demais 422; estoque final 0 |
| US2 | `GET /pedidos?clienteId=…&dataInicio=2026-01-01&dataFim=2026-01-31&page=1&pageSize=20` | página ordenada, totais corretos |
| US2 inválido | `page=0` ou `pageSize=101` ou período invertido | 400 |
| US3 | `GET /relatorios/top-clientes?dataInicio=2026-01-01&dataFim=2026-12-31` | 5 clientes, homônimos separados, valor decrescente |
| US5 | `GET /clientes`, `GET /produtos`, `GET /pedidos/{id}` | listas e detalhe; 404 para id inexistente |
| US1 idempotência | 2+ `POST` simultâneos com o mesmo `Idempotency-Key` e corpo | 1 pedido criado; demais devolvem o mesmo pedido; estoque baixado uma vez |
| US1 chave reutilizada | mesma chave com corpo diferente | 422 `chave_reutilizada` |
| Rate limit | rajada de `POST /pedidos` | 429 com `Retry-After` |

## 4. Cenário pela interface (US2 e US4)
Abrir http://localhost:8080 → ver a listagem com filtros → "Novo pedido" → escolher cliente e itens → confirmar → o pedido aparece no topo da listagem; repetir com estoque insuficiente e ver a mensagem sem fechar o formulário.

## 5. Testes
```bash
dotnet test backend                          # unitários, arquitetura e integração (Testcontainers; Docker necessário)
npm --prefix frontend test -- --run          # Vitest
npx --prefix frontend playwright test        # e2e contra o compose em execução
```
