# ADR-009 — Padrão único de erros: ProblemDetails com códigos estáveis

**Status:** Aceito · **Data:** 2026-11-13

## Contexto
Erros genéricos ("Erro ao processar") escondem a causa e impedem a interface de ajudar o usuário. A spec distingue validação, regra de negócio, inexistência e limite de taxa.

## Decisão
Toda resposta de erro é ProblemDetails (RFC 9457) com extensões `code` (estável, específico), `traceId`, `meta` e, no 400, `errors[]` com todos os problemas. Catálogo único no backend e dicionário único `code → mensagem pt-BR` no frontend, ambos descritos em `docs/policies/errors.md` e testados por código.

## Alternativas
- Só status HTTP + mensagem livre: o front teria de interpretar texto.
- Um código por endpoint: não reaproveita mensagens nem testes.

## Consequências
Novo erro exige entrada no catálogo, no contrato, no dicionário do front e testes (cobrado pelo `code-reviewer`). Em troca, mensagens específicas e consistentes e suporte por `traceId`.
