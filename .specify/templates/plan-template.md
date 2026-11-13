# Plano de Implementação: [FEATURE]

**Branch**: `[tipo/NNN-slug]` | **Data**: [DATA] | **Spec**: [link]

**Entrada**: especificação em `specs/[NNN-feature]/spec.md`

## Resumo

[Requisito principal da spec + abordagem técnica da pesquisa]

## Contexto Técnico

**Linguagem/Versão**: [ou NEEDS CLARIFICATION]
**Dependências principais**: [ou NEEDS CLARIFICATION]
**Armazenamento**: [ou N/A]
**Testes**: [ou NEEDS CLARIFICATION]
**Plataforma alvo**: [ou NEEDS CLARIFICATION]
**Tipo de projeto**: [web, biblioteca, CLI…]
**Metas de desempenho**: [específicas do domínio]
**Restrições**: [específicas do domínio]
**Escala/Escopo**: [específicos do domínio]
**Código existente**: [o que o `impact-analyzer` mapeou, ou "nenhum (greenfield)"]

## Constitution Check

*GATE: passa antes da Fase 0 e é reavaliado depois da Fase 1.* Uma linha por princípio de `.specify/memory/constitution.md`, com status OK ou violação justificada.

## Estrutura do Projeto

### Documentação (esta feature)

```text
specs/[NNN-feature]/
├── spec.md  plan.md  research.md  data-model.md  quickstart.md
├── contracts/
├── checklists/
└── tasks.md            (gerado por /speckit-tasks)
```

### Código-fonte (raiz do repositório)

[Árvore real de `backend/`, `frontend/` e `database/` afetados, com caminhos concretos.]

**Decisão de estrutura**: [justifique e referencie as pastas reais]

## Fatias de entrega (PRs)

| PR | Branch | Conteúdo |
|---|---|---|

## Acompanhamento de Complexidade

*Preencha só se o Constitution Check tiver violações a justificar.*

| Violação | Por que é necessária | Alternativa simples rejeitada porque |
|---|---|---|
