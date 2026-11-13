# ADR-003 — EF Core (escrita) + Dapper (leitura), sem migrations

**Status:** Aceito · **Data:** 2026-11-13

## Contexto
O schema precisa nascer de scripts SQL em `database/` executados no `docker compose up`, e a Parte 4 exige controle fino do SQL de relatório.

## Decisão
- **EF Core** só nos Commands: agregado `Pedido`, `IUnitOfWork`, transação e execution strategy. **Sem migrations**: o EF mapeia o schema dos scripts.
- **Dapper** nas Queries: SQL explícito, parametrizado e sargável, DTOs de saída.
- Baixa de estoque atômica (`UPDATE ... WHERE Estoque >= @q`) dentro da transação.

## Alternativas
- Só EF Core: menos dependências, menos controle do SQL do relatório.
- Só Dapper: transação e mapeamento manuais.
- EF com migrations: duplicaria a fonte do schema.

## Consequências
Duas formas de acesso a dados, separadas por papel (ver ADR-008). Mudança de schema exige script e mapeamento no mesmo PR.
