---
name: discover
description: Transforma uma ideia de produto do Gestão de Pedidos (feature nova, mudança de funcionalidade, novo fluxo) em um brief com problema, público, histórias de usuário e critérios de aceite, pronto para o /speckit-specify. Use quando o usuário trouxer uma ideia ainda vaga antes de existir spec.
---

# /discover — da ideia ao brief

Chegar a um brief claro **antes** da spec, sem escrever código e sem inventar requisitos. Perguntas curtas, uma rodada por vez.

## 1. Entender
- **Problema**: que dor resolve, para quem, como é feito hoje (confira no código e em `docs/domain.md`; cite `arquivo:linha`).
- **Público**: quem opera pedidos, quem consulta relatórios.
- **Resultado esperado** e **fora de escopo**.

## 2. Histórias e critérios
- "Como <público>, quero <ação>, para <benefício>".
- Critérios Dado / Quando / Então verificáveis: é com eles que o `qa-guardian` escreve os testes.
- Regras de negócio novas ou alteradas e conflitos com as atuais.

## 3. Impacto
Camadas (`backend`, `frontend`, `database`), mudança de contrato ou schema (confirme com `impact-analyzer`), riscos de `docs/known-issues.md`.

## 4. Fatiar
Entregas pequenas e independentes, na ordem de valor; cada fatia vira uma spec.

## Saída
Grave `specs/discovery/<AAAA-MM-DD>-<slug>.md` (pt-BR) com problema, público, resultado, fora de escopo, histórias + critérios, regras, impacto, riscos, fatias e decisões pendentes. Apresente o resumo e **pare para aprovação**. Aprovado → `/speckit-specify`.
