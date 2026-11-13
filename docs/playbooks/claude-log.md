# Diário de uso do Claude

Alimentado pelo `/retro` a cada PR; vira a seção "Como usei o Claude" do README.

## Bootstrap (2026-11-13)
- **Funcionou bem**: ler só a governança do repositório de referência (sem o código) e adaptar; agentes com papéis rígidos; guardrails executáveis em vez de regra só escrita.
- **Corrigido**: o hook `PreToolUse` bloqueava comandos legítimos porque lia o texto inteiro do comando (corpo de heredoc com `--no-verify` e com a regex de coautoria); passou a ignorar corpos de heredoc (`scripts/hooks/strip-heredocs.awk`). Regex inline com parênteses em `check-pr.sh` deu erro de sintaxe e foi movida para variável.

## PR 1: SDD (2026-11-13)
- **Funcionou bem**: o `/speckit-analyze` e o `code-reviewer` acharam falhas reais antes de existir código: job `e2e` que quebraria o CI do PR 2 sem frontend, `dotnet test` com dois projetos, rate limit por IP que veria só o nginx, FR-017 e FR-021 sem teste. Perguntas do usuário no meio do plano (concorrência, erros, MCP com credencial) melhoraram o desenho.
- **O Claude errou e foi corrigido**:
  - Desenho de idempotência com a chave gravada só no fim: com estoque exato, a segunda requisição receberia 422 em vez do pedido da primeira. Correção: `sp_getapplock` pela chave no início da transação (apontado pelo `code-reviewer`).
  - `.mcp.json` entrou com DSN e senha padrão; o guardrail só olhava `chave=valor`. Correção: `${MSSQL_MCP_DSN}` e regra contra credencial em URL (que, por sua vez, dava falso positivo em `${VAR}`, corrigido).
  - Arquivos herdados do projeto de referência (`known-issues.md`, `backlog.md`) e tela de relatório fora da spec: removidos (Princípio VII).
  - Scripts meus com falhas: regex inline em `[[ =~ ]]`, edição em lote que casou o trecho errado e duplicou o YAML (restaurado do git), numeração de tarefas com marcador que a regex não reconhecia.
- **Lição**: toda regra "só olha X" de guardrail precisa de teste com um caso que deve falhar e outro que deve passar.

## PR 2: backend (2026-11-13)
- **Funcionou bem**: `qa-guardian` escreveu 251 testes antes do código (unitários, arquitetura, integração com SQL Server real) e lacrou; o `code-reviewer` achou falhas que passavam nos testes: rate limit contornável com `X-Forwarded-For` forjado (a sub-rede inteira do compose era confiável, incluindo o gateway do host), `meta.disponivel` desatualizado quando se perde a corrida pelo último item, item `null` no JSON virando 500.
- **O Claude errou e foi corrigido**:
  - Confiar na sub-rede do compose em vez do IP do proxy: contradizia o ADR-010. Correção: encaminhamento desligado até existir o nginx, e `KnownNetworks` removido.
  - Disse que o item nulo já devolvia 400 sem verificar; o teste do `qa-guardian` provou o 500.
  - `PedidoInvalidoException` mapeada para um código fora do catálogo (`pedido_invalido`); passou a carregar códigos que já existem.
  - Retorno de `sp_getapplock` ignorado; `!` para silenciar nulos; doc exigia `meta.maximo` que o código nunca emitiu (doc corrigido).
- **Lição**: afirmação sobre comportamento sem requisição ou teste é hipótese; o teste de arquitetura também rejeita tipos gerados pelo compilador (`[x]` como argumento).
