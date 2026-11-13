# ADR-001 — Spec Kit, constitution, políticas e agentes próprios

**Status:** Aceito · **Data:** 2026-11-13

## Contexto
O teste avalia como se trabalha com IA: especificação antes do código, rastreabilidade e qualidade. O repositório `app-barbearia` já validou um conjunto de governança (constitution, agentes com papéis rígidos, guardrails, CI).

## Decisão
- **GitHub Spec Kit** (`.specify/`, skills `/speckit-*`) conduz o fluxo; specs em `specs/001-gestao-pedidos/`.
- **Constitution** própria (`.specify/memory/constitution.md`) com 7 princípios e gates.
- **Agentes** em `.claude/agents/`: só o `qa-guardian` escreve testes (lacrados por hash em `scripts/qa-seal.sh`); os demais são somente leitura e exigem evidência `arquivo:linha`. Acrescentado `sql-tuner` para a Parte 4.
- **Guardrails executáveis**: hooks git (`pre-commit`, `commit-msg`), hooks do Claude (`PreToolUse` e `PostToolUse` com build/lint), CI `governance`.
- **Skill `/retro`** fecha cada entrega: docs e memória.

## Alternativas
- Só `CLAUDE.md`, sem constitution: descartado; os gates do `/speckit-plan` dependem dela.
- Framework de agentes de terceiros: descartado por dependências desnecessárias.

## Consequências
Mais cerimônia por entrega, compensada por PRs temáticos e commits pequenos (ver ADR-005).
