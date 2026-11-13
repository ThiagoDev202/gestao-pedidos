# ADR-004 — Testes de integração com Testcontainers

**Status:** Aceito · **Data:** 2026-11-13

## Contexto
O enunciado exige integração contra SQL Server real. Subir o banco do compose para testes acopla a suíte ao ambiente do desenvolvedor.

## Decisão
`Testcontainers.MsSql` + `WebApplicationFactory` + Respawn: cada execução sobe um SQL Server efêmero, aplica `database/*.sql` e limpa os dados entre testes. Roda igual local e no GitHub Actions (Docker disponível).

## Alternativas
- Banco do compose: dependência do ambiente e estado compartilhado.
- SQL Server LocalDB ou SQLite/InMemory: não reproduzem T-SQL, índices nem concorrência.

## Consequências
Primeira execução baixa a imagem (lenta); o `IClassFixture` compartilha o container entre testes da mesma coleção.
