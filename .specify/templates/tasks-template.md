---
description: "Lista de tarefas da feature [NOME]"
---

# Tarefas: [NOME DA FEATURE]

**Entrada**: documentos de `specs/[NNN-feature]/`
**Pré-requisitos**: plan.md, spec.md; opcionais: research.md, data-model.md, contracts/, quickstart.md

**Testes**: obrigatórios (Constituição V). O `qa-guardian` escreve os testes **antes** do código, confirma que falham pelo motivo certo, lacra com `scripts/qa-seal.sh seal` e há um commit `test(...)` vermelho antes do `feat(...)`. Quem implementa não edita testes.

## Formato: `- [ ] T001 [P?] [US?] Descrição com caminho do arquivo`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência pendente)
- **[USn]**: história da spec (só nas fases de história)
- Caminhos reais: `backend/src/GestaoPedidos.<Camada>/…`, `backend/tests/…`, `frontend/src/…`, `database/…`
- Restrições do `data-model.md` (tamanho, obrigatoriedade, enum) transcritas literalmente na tarefa
- Sem comentários no código; domínio em pt-BR, termos técnicos em inglês

## Fase 1: Preparação (estrutura do projeto)

- [ ] T001 [descrição]

## Fase 2: Fundação (bloqueia todas as histórias)

- [ ] T0XX [descrição]

**Ponto de verificação**: a fundação compila e o teste de arquitetura passa.

## Fase 3: User Story 1 - [título] (P1)

**Objetivo**: [o que entrega] · **Teste independente**: [como validar]

### Testes (qa-guardian, RED)
- [ ] T0XX [P] [US1] [teste] em `backend/tests/…`
- [ ] T0XX [US1] Lacrar e commitar `test(...)`: `scripts/qa-seal.sh seal`

### Implementação
- [ ] T0XX [US1] [implementação] em `backend/src/…`

**Ponto de verificação**: a história funciona sozinha e os testes passam.

[Repita a fase por história, em ordem de prioridade]

## Fase Final: Gates da entrega

- [ ] T0XX `qa-guardian`: validação (`scripts/qa-seal.sh check`) com veredito APROVADO
- [ ] T0XX `ux-guardian` APROVADO (se houve UI)
- [ ] T0XX `scripts/check-guardrails.sh --diff origin/main` e `scripts/check-docs-drift.sh`
- [ ] T0XX `code-reviewer` sobre o diff final, sem Bloqueante
- [ ] T0XX `/retro` (docs, memória, `docs/playbooks/claude-log.md`)

## Dependências e ordem

[Dependências entre fases e histórias; oportunidades de paralelismo; estratégia (MVP primeiro, entrega incremental).]
