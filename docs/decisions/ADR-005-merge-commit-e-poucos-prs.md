# ADR-005 — Merge commit, poucos PRs temáticos e commits pequenos

**Status:** Aceito · **Data:** 2026-11-13

## Contexto
O teste analisa os commits para entender como a solução evoluiu e proíbe squash e reescrita de histórico. O custo fixo de cada PR (review, retro, CI) pesa quando são dezenas.

## Decisão
Bootstrap direto na `main` (repositório vazio) e 4 PRs temáticos (SDD, backend, frontend, query + entrega), cada um com muitos commits atômicos. Merge **sempre por merge commit**; squash e rebase desabilitados nas configurações do repositório. Divergência consciente do `app-barbearia`, que faz squash.

## Consequências
Histórico rico e legível (RED → GREEN por caso de uso); `code-reviewer` e `/retro` rodam por PR, não por commit.
