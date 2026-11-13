---
name: ship
description: Fluxo padrão de entrega do Gestão de Pedidos, do código ao merge — testes do qa-guardian, gates locais, code-reviewer, commits pequenos, PR, CI verde e só então merge commit. Use SEMPRE que for entregar uma feature, correção ou qualquer mudança (inclusive quando o usuário disser "abre o PR", "sobe isso", "faz o merge").
---

# /ship — do código ao merge

Ordem fixa. Se uma etapa falhar, **pare**, corrija na etapa certa e recomece dali. Nunca pule etapa, nunca use `--no-verify`, nunca faça merge com check vermelho.

## Início. Contexto da tarefa
Quando o usuário descrever o que quer, **não comece pelo código**:
- **Ideia vaga** → `/discover`; o brief aprovado vira a entrada do `/speckit-specify`.
- **Bug ou comportamento inesperado** → agente `bug-investigator` (causa raiz provada, menor patch); o caso de reprodução vira requisito do `qa-guardian`.
- **Feature, contrato da API ou schema** → `/speckit-specify` → `/speckit-clarify` → `/speckit-plan` (com `impact-analyzer` e, se arriscado, `architect`) → `/speckit-tasks` → `/speckit-analyze`. **Pare para aprovação humana** após a spec e após plano + tarefas.
- **Trilha leve** (correção sem mudar contrato nem schema, numa camada): uma frase do comportamento esperado e a causa raiz com `arquivo:linha` no PR; vira o requisito do `qa-guardian`. Se passar a tocar contrato, schema ou duas camadas, vira trilha completa.

## 0. Branch
- Nunca na `main` (protegida). `git switch -c {tipo}/{NNN}-{slug}`; `NNN` = número da spec ou `scripts/next-branch-number.sh`.
- `{tipo}` é tipo de commit (`feat`, `fix`, `docs`, `perf`…); `api`, `web`, `db`, `e2e`, `docker`, `ci`, `docs`, `ai` são **escopos** do título, nunca tipo de branch.
- Se `git status` mostrar mudanças que não são suas, use `git worktree add ../gestao-pedidos-<slug> -b {tipo}/{NNN}-{slug} origin/main`.
- O escopo do título define o que o PR pode tocar (`scripts/check-pr.sh`): `feat(db,api,docker)`.

## 1. Testes primeiro — `qa-guardian`
1. **RED** por caso de uso: o `qa-guardian` escreve os testes dos requisitos, confirma que falham pelo motivo certo e lacra (`scripts/qa-seal.sh seal`). Faça **um commit** `test(...)` com os testes vermelhos antes da implementação, para o histórico mostrar RED → GREEN (o hook de build do Claude e o CI só exigem verde no fim do PR; o commit RED deve compilar).
2. Implemente até passar, sem editar testes. **Sem comentários no código.** Teste parece errado: devolva ao `qa-guardian` com o motivo.
3. **Validação**: `qa-guardian` de novo (`scripts/qa-seal.sh check`). Só siga com `APROVADO`.
4. **UI alterada**: `ux-guardian` antes e depois. Só siga com `APROVADO`.
Mudança só de docs/configuração: diga explicitamente que não há teste a escrever e por quê.

## 2. Gates locais
| Camada | Comandos |
|---|---|
| backend | `dotnet format backend --verify-no-changes` · `dotnet build backend -warnaserror` · `dotnet test backend` |
| frontend | `npm --prefix frontend run lint` · `typecheck` · `npm --prefix frontend test -- --run` · `build` |
| e2e | `docker compose up --build --wait` · `npx --prefix frontend playwright test` |
| sempre | `scripts/check-guardrails.sh --diff origin/main` · `PR_TITLE=… PR_AUTHOR=… HEAD_REF=<branch> scripts/check-pr.sh origin/main` · `scripts/check-docs-drift.sh` |
Reporte falhas como estão; não diga "pronto" com gate vermelho.

## 3. Revisão — `code-reviewer` (uma vez por PR, sobre o diff final)
Achado **Bloqueante** ou `BLOQUEADO` → corrija e rode de novo. Corrigiu algo depois da revisão? Rode outra vez.

## 4. `/retro`
Docs e memória atualizados antes do commit final.

## 5. Commits — só com pedido explícito do usuário
- Pequenos e atômicos, que compilam, um assunto por commit, Conventional Commits em inglês, ≤ 72 caracteres. Alvo: 10 a 20 commits por PR temático.
- **Só no nome da pessoa**: sem `Co-Authored-By` de ferramenta nem "Generated with" (o `commit-msg` e o CI barram).

## 6. PR — só com pedido explícito
- `git push -u origin <branch>` só no fim do PR ou em marcos (cada push dispara o CI).
- Título no formato do commit principal, com escopo. Corpo pelo `.github/pull_request_template.md`: o que e por quê, spec, áreas, vereditos, como testar, rollback. **Sem marca de ferramenta.**

## 7. CI verde
`gh pr checks <n> --watch`. Obrigatórios: **`ci-ok`** e **`governance`**. Vermelho → `gh run view <id> --log-failed`, corrija na etapa certa e faça push.

## 8. Merge — só com pedido explícito e `ci-ok` verde
- A caixa do `code-reviewer` está marcada no corpo do PR e a revisão cobre o diff final.
- **Sempre merge commit** (preserva o histórico de commits pequenos; squash e rebase estão desabilitados): `scripts/merge-when-green.sh <n>`.
- Depois: `git switch main && git pull --ff-only`.
