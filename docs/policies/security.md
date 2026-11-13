# Política: segurança

- **SQL**: sempre parametrizado (EF Core ou parâmetros do Dapper). Concatenar entrada em SQL é Bloqueante.
- **Entrada**: todo request passa por validator (FluentValidation) e pelo domínio; limites explícitos (`pageSize` ≤ 100, quantidade máxima por item, número máximo de itens por pedido).
- **Mass assignment**: o preço do item nunca vem do cliente; DTOs de entrada não carregam campos que o servidor define (`Id`, `DataPedido`, `ValorTotal`, `PrecoUnitario`).
- **Erros**: ProblemDetails sem stack trace nem detalhe interno em produção; mensagens do domínio em pt-BR, sem vazar nomes de tabela ou SQL.
- **Rate limiting**: `POST /pedidos` com token bucket e limite global por IP (429 + `Retry-After`).
- **CORS**: só a origem do frontend (variável de ambiente); sem `AllowAnyOrigin` em produção.
- **Segredos**: nunca no repositório. `.env` fora do git; `.env.example` só com valores de desenvolvimento local. Senhas de produção em secrets do GitHub/ambiente.
- **Imagens**: executar como usuário não root; imagens base fixadas por versão; `.dockerignore` em cada Dockerfile.
- **MCP do banco**: somente leitura (`READONLY=true`), apenas contra o banco local de desenvolvimento.
- **Dependências**: Dependabot semanal; licença comercial (MediatR, FluentAssertions 8) proibida.
