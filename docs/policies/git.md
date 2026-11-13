# Política: git (branches, commits e PRs)

## Branches
`{tipo}/{NNN}-{slug}`: `tipo` é o tipo do commit; `NNN` é o número da spec em `specs/` (ou o próximo livre: `scripts/next-branch-number.sh`); `slug` curto, minúsculo, com hífens. Ex.: `feat/001-backend-pedidos`. `main` só recebe PR.

## Commits — Conventional Commits
```
<tipo>(<escopo>): <resumo no imperativo, em inglês, ≤ 72 caracteres>

<corpo opcional: o porquê, não o como>
```
Tipos: `feat`, `fix`, `refactor`, `perf`, `test`, `docs`, `style`, `build`, `ci`, `chore`.

| Escopo | Área |
|---|---|
| `api` | `backend/` |
| `web` | `frontend/` |
| `db` | `database/` |
| `e2e` | `frontend/e2e/` e `playwright.config.ts` |
| `docker` | `docker-compose.yml`, Dockerfiles, `nginx.conf` |
| `ci` | `.github/`, `scripts/`, `.githooks/` |
| `docs` | `docs/`, `README.md` |
| `ai` | `.claude/`, `.specify/`, `specs/`, `AGENTS.md`, `CLAUDE.md`, `.mcp.json`, `docs/policies`, `docs/decisions`, `docs/playbooks` |

- Escopos combinam: `feat(db,api,docker)`. Quebra de contrato: `!` após o escopo e `BREAKING CHANGE:` no corpo.
- **Um assunto por commit**, em ordem incremental; o projeto compila a cada commit. Ciclo RED → GREEN visível: `test(api): …` antes de `feat(api): …`.
- **Histórico preservado**: sem squash, sem rebase de branch publicada, sem force-push.
- **Só no nome da pessoa**: sem `Co-Authored-By` de ferramenta e sem "Generated with" em commits ou PRs.

## Antes de commitar
- `scripts/check-guardrails.sh` roda no hook `pre-commit`; o `commit-msg` valida o assunto. Ative uma vez por clone: `scripts/setup.sh` (ou `git config core.hooksPath .githooks`). O hook do Claude bloqueia commit sem hooks ativos e com a flag que pula hooks.
- Gates da camada alterada (`AGENTS.md`).

## Pull requests
- Poucos PRs, temáticos, com muitos commits pequenos.
- Título no formato do commit principal, com escopo. Corpo pelo template: o que/por quê, spec, áreas, vereditos, como testar, **rollback**.
- `code-reviewer` sem achado Bloqueante, rodado sobre o diff final; a caixa marcada no corpo do PR (o `governance` confere).
- Checks obrigatórios: **`ci-ok`** e **`governance`**.
- **Merge sempre por merge commit** (`scripts/merge-when-green.sh <n>`); squash e rebase desabilitados no repositório, para o histórico evoluir como foi feito. Branch apagada após o merge.

## Controle de área
`scripts/check-pr.sh` roda no `pr-governance.yml` (`pull_request_target`) com o script e o `.github/MAINTAINERS` da `main`, nunca os do PR. Barra: branch ou commit fora do padrão, coautoria de ferramenta, arquivo fora do escopo declarado, área protegida (`.github/`, `scripts/`, `.githooks/`, `.claude/`, `.specify/`, `AGENTS.md`, `CLAUDE.md`, `docs/policies`, `docs/decisions`) alterada por quem não é maintainer, código de `backend/src` ou `frontend/src` em PR `feat/fix/refactor/perf` sem teste, e corpo de PR sem a caixa do `code-reviewer`.
