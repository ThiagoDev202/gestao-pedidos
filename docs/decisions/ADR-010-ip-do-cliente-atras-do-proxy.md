# ADR-010 — IP do cliente atrás do proxy e rate limiting

**Status:** Aceito · **Data:** 2026-11-13

## Contexto
No Docker, o frontend (nginx) faz proxy de `/api` para a API. Sem tratamento, a API enxerga sempre o IP do container nginx, e o limite de taxa "por origem de rede" (FR-016) trataria todos os clientes como um só. O contrário também é um risco: aceitar `X-Forwarded-For` de qualquer origem deixa o cliente forjar o próprio IP e escapar do limite.

## Decisão
- O nginx repassa `X-Forwarded-For` e `X-Forwarded-Proto`.
- A API usa `UseForwardedHeaders` **somente** quando `ForwardedHeaders:Enabled=true`, e só confia em cabeçalhos vindos de IPs listados em `ForwardedHeaders:KnownProxies`; de qualquer outra origem o cabeçalho é ignorado.
- O particionamento do rate limiting usa o IP resultante (`HttpContext.Connection.RemoteIpAddress`).
- No `docker-compose.yml`, a rede do compose fixa a sub-rede para o nginx ter IP conhecido e entrar em `KnownProxies`. Enquanto não existe proxy (antes do serviço `web`), `ForwardedHeaders__Enabled` fica `false`; confiar na sub-rede inteira incluiria o gateway do host e permitiria forjar `X-Forwarded-For` para escapar do rate limit.

## Alternativas
- Confiar em qualquer `X-Forwarded-For`: forjável.
- Limitar por rota sem IP: um único cliente esgotaria o limite de todos.
- Não limitar: contraria FR-016.

## Consequências
Configuração explícita por ambiente (variáveis `ForwardedHeaders__*`) e teste de integração para o caso confiável e o não confiável.
