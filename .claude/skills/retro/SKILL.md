---
name: retro
description: Fecha qualquer solicitação ou tarefa do Gestão de Pedidos — avalia o que deve ir para a memória (MEMORY.md), atualiza os registros existentes e alinha a documentação ao código alterado (docs, ADRs, domain). Use SEMPRE ao final de uma tarefa, de cada PR, ou quando o usuário pedir para atualizar memória ou documentação.
---

# Retro: devolver o aprendizado para a documentação

O objetivo é que a próxima pessoa (ou agente) encontre os docs **iguais ao código**. Nada aqui é escrito sem conferir no código.

## 1. Levantar o que mudou
`git diff --stat` e `git diff` contra a base, mais `git status`. Se houver `specs/<NNN>/`, leia `spec.md`, `plan.md` e `tasks.md`.

## 2. Validar e atualizar o doc de cada área tocada
| Mudou em | Doc a revisar |
|---|---|
| `backend/src/Api` (endpoints) | `specs/<NNN>/contracts/openapi.yaml`, `docs/spec.md` |
| `backend/src/Domain`, `Application` | `docs/domain.md`, `docs/patterns/` |
| `database/*.sql` | `docs/spec.md` (modelo de dados), `docs/playbooks/query-debug.md` |
| `frontend/src` | `docs/policies/frontend.md` se surgiu padrão novo |
| variáveis de ambiente | `.env.example` e README |
| agentes, skills, scripts, CI, portas, comandos | `README.md`, `AGENTS.md`, `CLAUDE.md` (`scripts/check-docs-drift.sh`) |
Para cada afirmação escrita, confirme no código e cite o caminho. Doc antigo errado em algo que você não alterou: corrija também.

## 3. Riscos e decisões
- Problema encontrado e **não** resolvido: abra uma issue do GitHub (`gh issue create`) com gravidade, `arquivo:linha`, impacto e sugestão.
- Problema resolvido: cite a issue no PR (`Closes #N`).
- Decisão de arquitetura: `docs/decisions/ADR-NNN-<slug>.md` (contexto, decisão, alternativas, consequências) e índice em `docs/decisions/README.md`.
- **Registro para o README** ("Como usei o Claude"): anote em `docs/playbooks/claude-log.md` o que funcionou bem e o que o Claude errou e você corrigiu nesta entrega (achados do `code-reviewer` e do `qa-guardian`).

## 4. Memória (obrigatório em toda execução)
A memória do Claude Code deste projeto (`MEMORY.md` + um arquivo por fato) é o que o próximo agente lê primeiro.
1. Leia o `MEMORY.md` e os arquivos ligados ao que foi feito.
2. Avalie o que é novo e vale guardar: descobertas não óbvias, decisões e preferências do usuário, correções de forma de trabalhar, pegadinhas do ambiente, onde ficam recursos externos. Não guarde o que o repositório já registra (código, docs, git) nem o que só importa a esta conversa.
3. Atualize registros desatualizados (datas absolutas, caminhos verificados) antes de criar arquivo novo.
4. Remova ou corrija memórias que se provaram erradas.
5. `MEMORY.md` com uma linha por memória (`- [Título](arquivo.md) — gancho`).
6. Toda afirmação salva sobre o sistema precisa ter sido conferida no código nesta sessão.

## 5. Relatório
Liste memórias criadas/atualizadas/removidas (ou "nenhuma, porque…"), docs alterados, issues abertas e ADRs criados. Não faça commit sem pedido.
