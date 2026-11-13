# Gestão de Pedidos

Mini aplicação de cadastro e consulta de pedidos de clientes: **SQL Server + .NET 10 (Clean Architecture) + React/TypeScript**, tudo em Docker, desenvolvida com Spec-Driven Development e o Claude Code.

> Este README é a versão do bootstrap (toolkit de IA e governança). Rodar com Docker, testes, decisões técnicas, uso do MCP, Parte 4 e o relato "Como usei o Claude" entram completos no PR final de entrega.

## Estrutura

| Pasta | Conteúdo |
|---|---|
| `backend/` | API .NET: `Domain`, `Application`, `Infrastructure`, `Api` e testes (unit, arquitetura, integração) |
| `frontend/` | React + TypeScript + Vite, Tailwind + shadcn/ui, testes Vitest e e2e Playwright |
| `database/` | Scripts SQL de criação, schema, seed e a query da Parte 4 |
| `specs/` | Specs do Spec Kit (spec, plano, contrato OpenAPI, tarefas) |
| `docs/` | Domínio, políticas, ADRs, padrões, playbooks |
| `.claude/` | Agentes, skills e hooks do Claude Code |
| `.specify/` | Spec Kit e constitution |

## Toolkit de IA

Documentos centrais: [`AGENTS.md`](AGENTS.md) (fonte única das regras), [`CLAUDE.md`](CLAUDE.md) (navegador de docs e acordos de trabalho) e a [constitution](.specify/memory/constitution.md).

### Agentes (`.claude/agents/`)
| Agente | Papel |
|---|---|
| `qa-guardian` | único que escreve e aprova testes, a partir dos requisitos e antes do código; lacra a fase RED |
| `code-reviewer` | revisa o diff da branch contra spec, constitution e políticas |
| `architect` | valida planos arriscados contra código, ADRs e riscos |
| `impact-analyzer` | mapeia o raio de impacto (API, front, schema, testes, docs) |
| `bug-investigator` | causa raiz com evidência antes de qualquer correção |
| `ux-guardian` | valida telas: tokens, estados, formulários, acessibilidade |
| `sql-tuner` | investiga query com plano de execução via MCP (somente leitura) |

### Skills (`.claude/skills/`)
`/speckit-*` (Spec Kit) e as do projeto: `/discover` (ideia → brief), `/ship` (entrega do código ao merge), `/retro` (fecha a tarefa: docs e memória), `/onboard` (primeiro dia), `/docs-sweep` (docs × código), `/new-endpoint` (endpoint novo no padrão), `/new-screen` (tela nova no front) e `/sql-investigate` (query lenta ou incorreta).

### Hooks e guardrails
- **Git** (`.githooks/`, ativados por `scripts/setup.sh`): `pre-commit` roda `scripts/check-guardrails.sh` (segredos, `any`, comentários novos); `commit-msg` exige Conventional Commits e barra coautoria de ferramenta.
- **Claude** (`.claude/settings.json`): `PreToolUse` (`scripts/hooks/claude-pre-commit.sh`) bloqueia commit sem hooks git ou com a flag que os pula; `PostToolUse` (`scripts/hooks/claude-post-edit.sh`) roda build/lint depois de cada edição.
- **Testes lacrados**: `scripts/qa-seal.sh` grava o hash dos testes do `qa-guardian` e reprova teste alterado, fora do lacre ou apagado.
- **Auxiliares**: `scripts/check-pr.sh` (governança do PR), `scripts/check-docs-drift.sh` (docs × código), `scripts/next-branch-number.sh` e `scripts/merge-when-green.sh`.

### MCP do SQL Server
`.mcp.json` conecta o Claude ao SQL Server local pelo DBHub em modo **somente leitura** (`READONLY=true`), com o DSN em `MSSQL_MCP_DSN`. O uso durante o desenvolvimento será detalhado no README final.

### CI
`.github/workflows/pr-checks.yml` (check `ci-ok`: backend, frontend, e2e, docker e docs-drift conforme os caminhos alterados) e `pr-governance.yml` (check `governance`, com os scripts da `main`). Merge só por merge commit, preservando o histórico.

## Fluxo de trabalho

`/discover` → `/speckit-specify` → `/speckit-clarify` → `/speckit-plan` → `/speckit-tasks` → `/speckit-analyze` → `/speckit-implement` (com `qa-guardian` RED → código → validação) → `code-reviewer` → `/retro` → `/ship`. Detalhes em [`docs/policies/git.md`](docs/policies/git.md) e nas [decisões de arquitetura](docs/decisions/README.md).
