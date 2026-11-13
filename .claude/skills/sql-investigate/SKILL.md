---
name: sql-investigate
description: Fluxo para investigar uma query SQL Server lenta ou com resultado incorreto no Gestão de Pedidos (a Parte 4 do teste) — reproduzir, comparar resultados, medir com STATISTICS IO/TIME e plano de execução via MCP, levantar hipóteses, corrigir, indexar com evidência e registrar antes/depois. Use ao receber query com erro de resultado ou desempenho, ao propor índice ou ao otimizar uma query de relatório.
---

# /sql-investigate — query lenta ou incorreta

Acione o agente `sql-tuner` para a medição; esta skill é o roteiro e o registro.

1. **Isolar**: salve a query original em `database/04-query-debug-original.sql`.
2. **Corretude**: diga o que a query retorna × o que deveria (ver checklist do `sql-tuner`: LEFT JOIN anulado pelo WHERE, GROUP BY por coluna não única, ORDER BY sem DESC, função na coluna do filtro, ausência de TOP/desempate).
3. **Reproduzir** no seed local pelo MCP `sqlserver` (somente leitura): resultado da original × candidata, comparados com `EXCEPT`.
4. **Medir antes**: `SET STATISTICS IO, TIME ON` e plano real; anote leituras lógicas por tabela, scans, lookups, sorts, estimativa × real.
5. **Hipóteses** (2–4) com como provar cada uma; descarte com evidência.
6. **Corrigir**: reescrita sargável (intervalo semiaberto em vez de `YEAR()`), agrupamento por chave, `INNER JOIN` explícito, `TOP 5` com desempate. Salve em `database/05-query-debug-fixed.sql`.
7. **Indexar só com prova**: índice cobrindo o filtro e o join (`INCLUDE` para evitar lookup), declarado em `database/02-schema.sql`; diga o custo (escrita, espaço).
8. **Medir depois** e montar a tabela antes/depois.
9. **Registrar** em `docs/playbooks/query-debug.md` e resumir no README: o que estava errado, como investigou (plano, índices) e os números.
10. `qa-guardian` cobre a query corrigida com teste de integração (resultado esperado no seed); `/retro`.
