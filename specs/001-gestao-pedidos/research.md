# Pesquisa e Decisões: Mini Gestão de Pedidos

Sem `NEEDS CLARIFICATION` pendente: as duas decisões de escopo (produto repetido e limite de itens) foram tomadas com o usuário em 2026-11-13 e estão na spec.

## 1. Versão do .NET
- **Decisão**: .NET 10 LTS.
- **Razão**: o enunciado pede 8 ou superior; o suporte do .NET 8 termina em novembro de 2026.
- **Alternativas**: .NET 8 (suporte acabando); .NET 9 (STS, expira antes).

## 2. Estilo da API
- **Decisão**: Minimal APIs, um `MapGroup` por recurso, `TypedResults`, OpenAPI nativo + Scalar.
- **Razão**: endpoints finos que delegam a handlers de um caso de uso; menos cerimônia que controllers.
- **Alternativas**: controllers MVC.

## 3. Acesso a dados e CQRS
- **Decisão**: EF Core só nos Commands (agregado, transação) e Dapper nas Queries; sem migrations; mesmo banco; interfaces próprias `ICommandHandler`/`IQueryHandler`.
- **Razão**: invariantes e transação na escrita; SQL controlado e medido na leitura (Parte 4). MediatR tem licença comercial.
- **Alternativas**: só EF (menos controle do SQL do relatório); só Dapper (transação e mapeamento manuais); EF com migrations (duas fontes de schema).

## 4. Consistência do estoque sob concorrência
- **Decisão**: `UPDATE Produtos SET Estoque = Estoque - @q WHERE Id = @id AND Estoque >= @q` dentro da transação do pedido, em ordem crescente de `ProdutoId`; 0 linhas afetadas → recusa e rollback.
- **Razão**: a checagem e a baixa são uma operação atômica no banco (sem janela leitura-modifica-grava); a ordem fixa dos locks evita deadlock; o `CHECK (Estoque >= 0)` é a última defesa.
- **Alternativas**: `SELECT ... UPDLOCK` seguido de `UPDATE` (mais round-trips); concorrência otimista com `rowversion` (retry em conflito, mais complexo); isolamento `SERIALIZABLE` (menos vazão).
- **Preço**: lido na mesma transação; edição de preço está fora de escopo, então não há corrida relevante.

## 4b. Idempotência do POST /pedidos
- **Decisão**: header opcional `Idempotency-Key` (UUID). Tabela `IdempotenciaPedidos(Chave PK, HashCorpo, PedidoId, CriadoEm)` gravada na mesma transação do pedido, com `sp_getapplock` pela chave no início da transação para serializar reenvios simultâneos antes de tocar o estoque; reenvio com o mesmo hash devolve o pedido original (200 com o mesmo corpo); hash diferente → 422 `chave_reutilizada`; a PK da chave é a última defesa.
- **Razão**: dois pedidos idênticos são legítimos, então a deduplicação não pode ser pelo conteúdo; o risco real é o mesmo envio repetido (duplo clique, retry após timeout, queda logo após o commit, onde o retry do EF não sabe se gravou). A chave torna o reenvio seguro.
- **Alternativas**: só desabilitar o botão e limitar a taxa (não cobre retry de rede); deduplicar por conteúdo + janela de tempo (bloqueia compras repetidas legítimas); expirar chaves por job (fora de escopo; fica como próximo passo no README).
- **Isolamento**: `READ COMMITTED` basta; a consistência vem do update atômico do estoque, da PK da chave e do `UNIQUE` dos itens.

## 5. Resiliência
- **Decisão**: `EnableRetryOnFailure` do EF Core com a transação dentro de `strategy.ExecuteAsync` (reexecuta a unidade inteira, sem baixar estoque duas vezes; deadlock 1205 também é reexecutado); pipeline `Microsoft.Extensions.Resilience` (retry com backoff) nas leituras Dapper, só para erros transitórios do SQL; rate limiting de entrada com `AddRateLimiter` (token bucket no `POST /pedidos` e limite global por IP, 429 + `Retry-After`); sem circuit breaker.
- **Razão**: única dependência é o banco; circuito aberto só trocaria um erro por outro (ADR-007). Polly limita chamadas de saída, não entrada, por isso o rate limiter nativo.
- **Alternativas**: circuit breaker (sem ganho com dependência única); rate limiting via Polly (modelo errado).

- **IP do cliente atrás do nginx**: a API usa `UseForwardedHeaders` com proxies confiáveis configuráveis; sem isso o limite por IP trataria todos os clientes como o container do nginx (ADR-010, criado no PR 2). Cabeçalho vindo de origem não confiável é ignorado.

## 6. Fuso e datas
- **Decisão**: `DataPedido` em UTC (`datetime2`) via `IClock`; filtros por data (`yyyy-MM-dd`) viram `[inicio 00:00 UTC, fim + 1 dia)`; o frontend exibe no fuso local.
- **Razão**: evita ambiguidade e permite `WHERE DataPedido >= @i AND DataPedido < @f` sargável.
- **Alternativas**: `datetimeoffset` (mais pesado sem necessidade); fuso do servidor (frágil).

## 7. Erros e códigos HTTP
- **Decisão**: ProblemDetails (RFC 9457). 400 validação (raiz `validacao_falhou` com `errors[]` de `{campo, code, mensagem}`), 422 regra de negócio (com `code`: `cliente_inexistente`, `produto_inexistente`, `estoque_insuficiente`, `chave_reutilizada`), 404 pedido inexistente, 429 excesso de requisições, 201 + `Location`.
- **Razão**: separa "dados malformados" de "dados válidos que violam regra", como pede a spec (FR-015).

- **Padrão completo** (campos, catálogo de códigos, `meta`, regras de backend e frontend): `docs/policies/errors.md` e ADR-009. Erros de validação listam todos os problemas; cada `code` é estável e tem mensagem específica na interface.

## 8. Paginação
- **Decisão**: `page`/`pageSize` com `OFFSET/FETCH` e `ORDER BY DataPedido DESC, Id DESC` (desempate estável), mais `COUNT` para totais.
- **Razão**: simples e suficiente para ~50 mil linhas; o índice `IX_Pedidos_DataPedido` cobre ordenação e filtro.
- **Alternativas**: keyset pagination (melhor em milhões, mas o enunciado pede páginas numeradas).

## 9. Índices (hipótese a medir no PR 4)
- `IX_Pedidos_ClienteId_DataPedido (ClienteId, DataPedido) INCLUDE (ValorTotal)` para o filtro por cliente + período.
- `IX_Pedidos_DataPedido INCLUDE (ClienteId, ValorTotal)` para período sem cliente e para o top clientes.
- `IX_ItensPedido_PedidoId INCLUDE (ProdutoId, Quantidade, PrecoUnitario)` para detalhe e para o join do relatório original da Parte 4.
- `IX_ItensPedido_ProdutoId` para a FK de produto.
- **Razão**: cada um serve a uma consulta real; serão confirmados ou removidos com `STATISTICS IO` e plano real (Princípio VI).

## 10. Testes de integração
- **Decisão**: `WebApplicationFactory` + `Testcontainers.MsSql` + Respawn; o container aplica `database/*.sql`; rate limiting configurável por opções (desligado nos testes gerais, ligado no teste de 429).
- **Razão**: SQL Server real, igual local e no CI; nenhuma dependência do banco do compose.

## 11. Frontend
- **Decisão**: Vite + React + TS strict, Tailwind + shadcn/ui, TanStack Query, react-hook-form + zod; filtros e página na URL; nginx serve o build e faz proxy de `/api` para a API (sem CORS no Docker).
- **Razão**: componentes acessíveis prontos, cache de servidor e formulários tipados; URL compartilhável nos filtros.

## 12. Docker
- **Decisão**: `sqlserver` (2022, healthcheck com `sqlcmd`), `db-init` (mssql-tools executa `database/*.sql` em ordem e sai com 0), `api` (5080) e `web` (8080), com `depends_on` por saúde/conclusão.
- **Razão**: um único `docker compose up`, schema e seed sem passo manual (FR-020).
