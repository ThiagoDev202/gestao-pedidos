---
name: onboard
description: Prepara o ambiente de quem chega ao projeto Gestão de Pedidos e vai desenvolver com o Claude Code — ativa os hooks git, confere .NET, Node, Docker, jq e gh, cria o .env e explica o fluxo de trabalho (spec, agentes, /ship). Use quando a pessoa acabou de clonar o repositório, perguntar "como eu começo?" ou quando os hooks do projeto não estiverem ativos.
---

# /onboard — primeiro dia

## 1. Ambiente
Rode `scripts/setup.sh` (idempotente): ativa `core.hooksPath=.githooks`, confere .NET 10, Node 20+, Docker Compose, `jq` e `gh`, e cria o `.env` a partir do `.env.example`. Reporte o que falhou como está; não instale nada sem a pessoa pedir.

## 2. Subir o sistema
`docker compose up --build`. Frontend em http://localhost:8080, API em http://localhost:5080/scalar. O banco sobe com schema e dados de exemplo aplicados por `database/*.sql`.

## 3. Apresentar o fluxo
- O código é a fonte da verdade; docs são hipótese. Leia `AGENTS.md` e, para a sua área, o doc em `docs/` (índice no `CLAUDE.md`).
- Feature/contrato/schema → Spec Kit (`/speckit-*`); correção pequena → trilha leve.
- Agentes são acionados sozinhos; só o `qa-guardian` escreve testes. Entrega pela `/ship`; tarefa fecha com `/retro`.
- Commits só no nome da pessoa; Conventional Commits com escopo.

## 4. Primeira tarefa sugerida
Subir a stack, criar um pedido pela tela e abrir a mesma consulta no MCP do SQL Server (`.mcp.json`).
