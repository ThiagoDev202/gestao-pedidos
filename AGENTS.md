# AGENTS.md

Instruções compartilhadas por todo assistente de código que trabalha neste repositório (Claude Code, Cursor, Copilot, …).

**Regra de ouro: o código é a fonte da verdade.** Nunca confie em um doc (inclusive este) sem conferir no código; cite `arquivo:linha`. Se um doc estiver errado, corrija-o na mesma mudança.

## Projeto

**Gestão de Pedidos**: cadastro e consulta de pedidos de clientes. Duas camadas independentes no mesmo repositório, sem código compartilhado:

| Camada | Stack | Pasta |
|---|---|---|
| Backend | .NET 10 · Minimal APIs · EF Core + Dapper · SQL Server 2022 | `backend/` (porta 5080, Scalar em `/scalar`) |
| Frontend | React · TypeScript · Vite · Tailwind + shadcn/ui · TanStack Query | `frontend/` (porta 8080 no Docker, 5173 em dev) |
| Banco | Scripts SQL idempotentes (criação, schema, seed) | `database/` |

Domínio e regras de negócio: `docs/domain.md`. Débitos e riscos conhecidos: `docs/known-issues.md`.

## Governança (Spec-Driven Development)

- **Constitution**: `.specify/memory/constitution.md` (7 princípios e gates). Specs, planos, tarefas e docs em **pt-BR**; identificadores do domínio em pt-BR, termos técnicos e commits em **inglês**.
- **Políticas** (leia antes de implementar): `docs/policies/coding.md`, `security.md`, `database.md`, `frontend.md`, `testing.md`, `errors.md`, `git.md`.
- **Fluxo** para features, mudança de contrato da API e de schema: `/speckit-specify` → `/speckit-clarify` → `/speckit-plan` → `/speckit-tasks` → `/speckit-analyze` → `/speckit-implement`. Pare para aprovação humana após a spec, após plano + tarefas e antes de commit/push. Correção que não muda contrato nem schema segue a trilha leve (uma frase de comportamento esperado e causa raiz com `arquivo:linha` no PR).
- **Agentes** (`.claude/agents/`), acionados automaticamente: `qa-guardian` (o único que escreve, altera e aprova testes, a partir dos requisitos e antes do código; veredito APROVADO/REPROVADO), `code-reviewer` (diff de toda branch), `ux-guardian` (toda mudança de UI), `impact-analyzer` (raio de impacto de API/schema), `architect` (planos arriscados), `bug-investigator` (causa raiz antes de qualquer correção), `sql-tuner` (investigação de query com plano de execução, via MCP).
- **Skills**: `/discover` (ideia → brief), `/ship` (entrega), `/retro` (fecha cada tarefa: docs e memória), `/onboard` (primeiro dia), `/docs-sweep` (docs × código), `/new-endpoint` (endpoint novo no padrão), `/new-screen` (tela nova no front), `/sql-investigate` (query lenta ou incorreta).
- **Entrega**: `/ship` — branch → testes → gates → review → PR → CI verde → merge commit. Poucos PRs temáticos com muitos commits pequenos e atômicos.
- **Git**: `main` protegida (só PR, só merge commit). Branches `{tipo}/{NNN}-{slug}`; Conventional Commits (`feat`, `fix`, `refactor`, `perf`, `test`, `docs`, `style`, `build`, `ci`, `chore`; escopos `api`, `web`, `db`, `e2e`, `docker`, `ci`, `docs`, `ai`, combináveis: `feat(db,api)`). Commits só no nome da pessoa: sem `Co-Authored-By` de ferramenta nem "Generated with".
- **CI** (checks exigidos `ci-ok` e `governance`): `.github/workflows/pr-checks.yml` roda backend, frontend, e2e e docker conforme os caminhos alterados; `.github/workflows/pr-governance.yml` roda guardrails e `scripts/check-pr.sh` com os scripts e `.github/MAINTAINERS` da `main`.
- **Guardrails**: `scripts/check-guardrails.sh` roda no hook git `pre-commit` (`git config core.hooksPath .githooks`, uma vez por clone, feito por `scripts/setup.sh`); o `commit-msg` exige Conventional Commits (≤ 72 caracteres) e barra coautoria de ferramenta. Os hooks do Claude (`.claude/settings.json`) bloqueiam commit sem hooks git ativos ou com `--no-verify`, e rodam o build/lint depois de cada edição (`scripts/hooks/claude-post-edit.sh`). `scripts/qa-seal.sh` lacra e confere os testes escritos pelo `qa-guardian`.
- **Memória**: `/retro` atualiza a memória do Claude (`MEMORY.md`) ao fim de cada tarefa. Notas pessoais vão para a memória, não para `docs/`.

## Comandos

```bash
docker compose up --build                  # sqlserver + db-init + api + web
scripts/setup.sh                           # primeiro dia: hooks git, checagem de ferramentas, .env

dotnet build backend -warnaserror
dotnet test backend                        # unitários + arquitetura + integração (Docker necessário)
dotnet format backend --verify-no-changes

npm --prefix frontend run dev | lint | typecheck | test | build
npx --prefix frontend playwright test      # e2e contra a stack do compose
```

## Padrões de código

### Todas as camadas
- **Sem comentários no código**: de linha, bloco ou XML-doc (`///`), em qualquer pacote, script ou workflow. Nomes explicam o código; o porquê vai no commit, no PR ou na spec. Só diretivas de ferramenta (`eslint-disable`, `@ts-expect-error`, `#pragma`, `SuppressMessage`). O guardrail barra.
- Sem URL, senha ou segredo fixos: variável de ambiente + `.env.example`.
- Sem `any` no TypeScript; tratamento explícito de `null` em C# (`Nullable` habilitado, warnings como erro).

### Backend (`backend/`)
- Camadas: `Domain` (entidades, agregado `Pedido`, exceções de domínio, zero dependências) ← `Application` (Commands/Queries, Handlers, Validators, ports) ← `Infrastructure` (EF Core `DbContext`, repositórios, queries Dapper, `IUnitOfWork`, `IClock`) e `Api` (Minimal APIs, DI, ProblemDetails, OpenAPI).
- Um caso de uso = uma pasta em `Application/<Agregado>/{Commands|Queries}/<Caso>/` com request, handler e validator. Handlers implementam `ICommandHandler<,>` ou `IQueryHandler<,>`; validação e log entram por Decorator no DI.
- Endpoints em `Api/Endpoints/<Recurso>Endpoints.cs` com `MapGroup`, `TypedResults`, sem regra de negócio.
- Escrita: agregado + EF Core + `IUnitOfWork`. Leitura: Dapper com SQL parametrizado e sargável, devolvendo DTOs. Nunca expor entidade de domínio na API.
- Estoque: baixa atômica no banco (`UPDATE ... WHERE Estoque >= @quantidade`), nunca leitura-modifica-grava.
- Erros: padrão único em `docs/policies/errors.md` (ProblemDetails com `code` estável, `traceId`, `meta` e `errors[]`); exceção de domínio tipada → 422, validação → 400 com todos os problemas; catálogo único em `Api/Errors/ErrorCatalog.cs`, sem montar ProblemDetails à mão.
- Nomes: domínio em pt-BR (`Pedido`, `CriarPedidoCommand`), sufixos técnicos em inglês (`PedidoRepository`, `CriarPedidoHandler`).

### Frontend (`frontend/`)
- `src/features/<feature>/{api,components,hooks,schemas,pages}`; `src/shared/{api,components/ui,lib}`; `src/app` (providers, router).
- HTTP só em `api/`; estado de servidor com TanStack Query; formulários com react-hook-form + zod; componentes shadcn/ui em `shared/components/ui`.
- Erros: tudo passa por `ApiError` (`shared/api/http-client.ts`) e pelo dicionário `code → mensagem pt-BR` (`shared/api/error-messages.ts`); erro de campo no campo, erro de regra no alerta do formulário, 500 com `traceId`; sem mensagem escrita à mão na tela.
- Estilo só por classes Tailwind e tokens (CSS vars); sem cor ou tamanho fixos. Estados vazio, carregando, erro e sucesso em toda tela. Botão desabilitado durante envio. Textos em pt-BR.

### Banco (`database/`)
- Scripts numerados e idempotentes (`IF NOT EXISTS`). Nunca editar o banco à mão; o schema muda só por script versionado. Índices com justificativa (`docs/policies/database.md`).

## Regras de negócio que não podem quebrar

- Pedido só é criado para cliente existente, com ≥ 1 item, cada quantidade > 0 e estoque suficiente para todos os itens; tudo ou nada (transação).
- O preço do item vem de `Produtos.Preco` no momento da criação (o cliente da API não envia preço) e fica congelado em `ItensPedido.PrecoUnitario`.
- Criar o pedido baixa o estoque; dois pedidos concorrentes pelo último item: só um passa.
- Reenvio com a mesma `Idempotency-Key` e o mesmo corpo devolve o pedido original; corpo diferente é recusado (422).
- Datas em UTC; filtros de período são datas inclusivas, convertidas para intervalo semiaberto `[inicio, fim + 1 dia)`.

## Ambiente e segredos

- Nunca commite `.env`, chaves ou senhas. Valores locais ficam em `.env` (cópia de `.env.example`).
- O MCP do SQL Server (`.mcp.json`) é **somente leitura** e usa o DSN da variável `MSSQL_MCP_DSN`.

| Serviço local | URL |
|---|---|
| Frontend | http://localhost:8080 |
| API / Scalar | http://localhost:5080 · http://localhost:5080/scalar |
| SQL Server | `localhost:1433` |
