---
name: docs-sweep
description: Varredura periódica de documentação × código do Gestão de Pedidos — confere cada afirmação factual dos docs (caminhos, comandos, portas, rotas, variáveis, agentes, skills, jobs do CI) contra o código e corrige o que divergiu. Use periodicamente, depois de refactors grandes ou quando suspeitar de doc velho.
---

# /docs-sweep — docs × código

1. Liste os docs: `README.md`, `AGENTS.md`, `CLAUDE.md`, `docs/**`, `specs/**`.
2. Para cada afirmação verificável, confirme no código e registre `afirmação → evidência (arquivo:linha)`: caminhos e pastas, comandos (rode os seguros), portas e URLs, rotas e contratos (`contracts/openapi.yaml` × `backend/src/Api/Endpoints`), tabelas e índices (`database/*.sql`), variáveis (`.env.example` × `docker-compose.yml` × código), agentes e skills (`.claude/`), jobs do CI (`.github/workflows`).
3. Rode `scripts/check-docs-drift.sh`.
4. Corrija o que divergiu; o que não deu para verificar vai marcado "não verificado".
5. Dados que apodrecem (contagem de testes, versões) trocam por apontador para a fonte.
6. Relatório: divergências achadas, corrigidas e pendentes. Não faça commit sem pedido.
