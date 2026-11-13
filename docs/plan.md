# Plano de implementação

Entregável exigido pelo teste: o plano gerado com o Claude em plan mode, mais o que foi ajustado ou contestado e por quê. O plano técnico detalhado do Spec Kit está em [`specs/001-gestao-pedidos/plan.md`](../specs/001-gestao-pedidos/plan.md) (e `research.md`, `data-model.md`, `contracts/`, `tasks.md`).

## 1. Plano gerado em plan mode

O Claude, em plan mode, leu o enunciado e a governança do repositório de referência (só o setup de IA, sem o código) e propôs:

- **Fase 0, toolkit de IA**: `CLAUDE.md` → `AGENTS.md` como fonte única; constitution do Spec Kit; 7 agentes com papéis rígidos (só o `qa-guardian` escreve testes, lacrados por hash); skills `/ship`, `/retro`, `/discover`, `/onboard`, `/docs-sweep` e as do projeto (`/new-endpoint`, `/new-screen`, `/sql-investigate`); hooks git e do Claude; CI com `ci-ok` + `governance`; MCP do SQL Server somente leitura.
- **Fase 1, SDD**: spec → clarify → plan → tasks → analyze, com pausa humana após a spec e após plano + tarefas.
- **Fase 2, implementação**: banco (scripts SQL idempotentes + seed), backend .NET (Clean Architecture, CQRS leve, EF na escrita e Dapper na leitura), frontend React/TS, Playwright, Parte 4 (query), CI/CD e README.
- **Padrões**: Clean Architecture verificada por teste de arquitetura, SOLID, GoF (Command, Decorator, Factory Method, Adapter, Builder), baixa de estoque atômica, retry de transientes e rate limiting.
- **Entrega**: um PR por fatia, com testes antes do código.

## 2. O que ajustei ou contestei, e por quê

| # | Ponto | O plano inicial / o enunciado | O que foi feito | Por quê |
|---|---|---|---|---|
| 1 | Número de PRs | um PR por fatia (~8) | bootstrap direto na `main` + 4 PRs temáticos, com muitos commits pequenos | o avaliador lê a evolução pelos commits; cada PR tem custo fixo de revisão, retro e CI |
| 2 | Estratégia de merge | squash (como no repositório de referência) | **somente merge commit**; squash e rebase desabilitados | o teste pede histórico preservado, sem squash nem reescrita |
| 3 | Coautoria nos commits | o ambiente sugere `Co-Authored-By` de ferramenta | commits e PRs só no nome do autor; `commit-msg`, hook do Claude e CI bloqueiam | decisão explícita; o uso do Claude fica evidente pelo `.claude/`, specs, README e vídeo |
| 4 | Versão do .NET | ".NET 8 ou superior" | **.NET 10 LTS** | o suporte do .NET 8 termina em novembro de 2026 |
| 5 | Idioma | código em inglês no repositório de referência | domínio em pt-BR (Pedido, ItemPedido…), termos técnicos e commits em inglês | o enunciado fixa tabelas, colunas e rotas em português; evita tradução constante |
| 6 | Persistência | só ORM ou só SQL | EF Core (escrita) + Dapper (leitura), sem migrations | transação e agregado na escrita; SQL controlado na leitura e na Parte 4; o schema nasce dos scripts exigidos |
| 7 | Integração com SQL Server | "Testcontainers ou o banco do compose" | **Testcontainers** | independe do ambiente do desenvolvedor e roda igual no CI |
| 8 | Resiliência | Polly com retry, circuit breaker e rate limiting (sugerido como opcional) | retry de transientes + rate limiting **nativo**; **sem circuit breaker** | só há uma dependência (o banco); circuito aberto trocaria um erro por outro; Polly limita chamadas de saída, não entrada (ADR-007) |
| 9 | CQRS | não pedido | adotado na versão leve, sem MediatR | escrita com regras e leitura com SQL afinado; MediatR tem licença comercial (ADR-008) |
| 10 | Endpoints de apoio | só os 3 do enunciado | acrescentei `GET /clientes`, `GET /produtos`, `GET /pedidos/{id}` e `/health` | o formulário precisa de clientes e produtos com estoque; o `Location` do 201 precisa apontar para algo |
| 11 | `ValorTotal` | derivável dos itens | persistido em `Pedidos` | listagem e relatório sem somar itens a cada consulta |
| 12 | Concorrência | não mencionada | baixa atômica (`UPDATE ... WHERE Estoque >= @q`) em ordem fixa de produtos, `CHECK (Estoque >= 0)`, teste com 50 pedidos simultâneos | evitar venda dupla e deadlock |
| 13 | Idempotência | não mencionada | `Idempotency-Key` com tabela própria | dois pedidos idênticos são legítimos; o risco real é o mesmo envio repetido (duplo clique, retry de rede) |
| 14 | Erros | "validação" genérica | padrão único ProblemDetails com `code` específico, `traceId`, `meta` e `errors[]`, e dicionário de mensagens no front | erros específicos e consistentes na API e na interface (ADR-009) |
| 15 | Rate limit atrás do nginx | não considerado | `UseForwardedHeaders` com proxies confiáveis (ADR-010) | sem isso o limite por IP trataria todos como o container do nginx; apontado pelo `/speckit-analyze` |
| 16 | Tela de top clientes | incluída no plano inicial | **removida** | a spec não a pede (Princípio VII); o relatório fica na API |
| 17 | Arquivos herdados | `known-issues.md` e `backlog.md` | removidos; pendências viram issues do GitHub | eram específicos do projeto de referência |
| 18 | Spec Kit | instalação padrão | removidos o motor de `workflows/` e a skill `taskstoissues`; templates reescritos em pt-BR com os gates do projeto | só o que o fluxo usa; templates em inglês gerariam specs fora do idioma do projeto |
| 19 | `.mcp.json` | DSN padrão com senha | só `${MSSQL_MCP_DSN}`, vindo do `.env`; guardrail passou a barrar credencial em URL | credencial não se versiona; o guardrail anterior só olhava `chave=valor` |

## 3. Fatias de entrega

| PR | Conteúdo |
|---|---|
| 0 (direto na `main`) | toolkit de IA, guardrails, CI de governança, constitution, agentes, skills, políticas e ADRs |
| 1 `docs/001-sdd-gestao-pedidos` | spec, plano, contrato, tarefas, este documento |
| 2 `feat/001-backend-pedidos` | banco, compose do SQL, backend completo e testes unit/arquitetura/integração |
| 3 `feat/002-frontend-pedidos` | frontend, camada de erros, nginx, Vitest, Playwright |
| 4 `perf/003-query-ranking-e-entrega` | Parte 4 (query), CD no GHCR, README final, roteiro do vídeo |

Cada PR: `qa-guardian` (testes antes do código, lacrados), implementação, `code-reviewer` no diff final, `/retro`, CI `ci-ok` + `governance` verdes e merge por merge commit.
