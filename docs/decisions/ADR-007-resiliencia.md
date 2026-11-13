# ADR-007 — Resiliência: retry e rate limiting, sem circuit breaker

**Status:** Aceito · **Data:** 2026-11-13

## Contexto
A única dependência externa da API é o SQL Server. Polly oferece retry, circuit breaker e rate limiter (de saída).

## Decisão
- **Retry** de erros transitórios do SQL: `EnableRetryOnFailure` do EF Core (com a transação do pedido dentro de `strategy.ExecuteAsync`) e pipeline `Microsoft.Extensions.Resilience` (Polly) nas leituras Dapper.
- **Rate limiting de entrada** com o middleware nativo `AddRateLimiter`: token bucket em `POST /pedidos` e limite global por IP, respondendo 429 com ProblemDetails e `Retry-After`.
- **Sem circuit breaker**: com um único banco e nenhuma chamada a serviço externo, o circuito aberto só trocaria um erro por outro e adicionaria um estado a testar. Reavaliar se surgir dependência externa.

## Consequências
Resiliência proporcional ao risco real; retry limitado para não amplificar carga.
