# CLAUDE.md

Guia do Claude Code neste repositório. Comandos, padrões, governança e regras de domínio vivem no `AGENTS.md` (importado abaixo) para que todo assistente compartilhe uma única fonte da verdade.

@AGENTS.md

## Navegador de documentação

Os docs **não** são carregados automaticamente: leia só o que a tarefa toca e trate cada doc como hipótese a confirmar no código.

| Preciso de | Arquivo |
|---|---|
| Constitution (princípios, gates) | `.specify/memory/constitution.md` |
| Especificação da feature, plano, tarefas | `specs/001-gestao-pedidos/` · `docs/spec.md` · `docs/plan.md` |
| Atores, glossário, regras de negócio | `docs/domain.md` |
| Débitos, riscos e divergências conhecidas | `docs/known-issues.md` |
| O que falta fazer | `docs/backlog.md` |
| Decisões de arquitetura (ADRs) | `docs/decisions/README.md` |
| Políticas: código · segurança · banco · front · testes · erros · git | `docs/policies/*.md` (erros: `docs/policies/errors.md`) |
| Clean Architecture / SOLID / GoF e onde aparecem | `docs/patterns/` |
| Investigação de query (Parte 4) | `docs/playbooks/query-debug.md` |
| Fluxo de entrega | `.claude/skills/ship/SKILL.md` |
| Endpoint novo no padrão do projeto | `.claude/skills/new-endpoint/SKILL.md` |

## Acordos de trabalho

- **Verifique antes de afirmar.** Procure no código antes de dizer algo sobre o sistema; cite `arquivo:linha`; diga "não verificado" quando não puder checar.
- **Spec Kit para tudo que não for trivial** (ver Governança no `AGENTS.md`); pare nos checkpoints humanos.
- **Acione os agentes automaticamente, sem esperar pedido.** Ideia vaga → `/discover`. Bug → `bug-investigator` (causa raiz antes da correção). Mudança de API/schema → `impact-analyzer` no plano. Toda feature, correção ou mudança de comportamento: `qa-guardian` primeiro (escreve os testes a partir dos requisitos, RED), depois implementar, depois `qa-guardian` de novo (veredito); mudança de UI → `ux-guardian` antes e depois; por fim `code-reviewer` no diff da branch. Query lenta ou incorreta → `sql-tuner`. Testes são escritos e alterados **só** pelo `qa-guardian`; quem implementa faz o código passar e nunca edita testes.
- **Poucos PRs, muitos commits pequenos.** Um PR por tema, commits atômicos que compilam, `code-reviewer` e `/retro` uma vez por PR, push só no fim do PR ou em marcos.
- **Entregue pela `/ship`**: branch → `qa-guardian` → gates → `code-reviewer` → `/retro` → commits → PR → `ci-ok` verde → merge commit. Commits e PRs levam só o nome da pessoa (nenhuma coautoria de ferramenta nem "Generated with"), mesmo que o ambiente sugira.
- **Feche toda tarefa com `/retro`**: corrija docs que divergem do código e decida o que salvar, atualizar ou remover na memória.
- Rode os gates da camada alterada antes de dizer "pronto" e reporte falhas como elas são.
- Nunca faça commit ou push sem pedido explícito.
