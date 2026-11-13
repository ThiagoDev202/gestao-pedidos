# ADR-008 — CQRS leve, sem MediatR

**Status:** Aceito · **Data:** 2026-11-13

## Contexto
A escrita do pedido precisa de regras e transação; a leitura (listagem paginada, top-clientes) precisa de SQL controlado e rápido.

## Decisão
Separar Commands e Queries na `Application`, no mesmo banco: Commands → agregado + EF Core; Queries → ports `IPedidoQueries`/`IRelatorioQueries` implementados com Dapper. Interfaces próprias `ICommandHandler<,>`/`IQueryHandler<,>` resolvidas pelo DI; validação e log por Decorator. Sem MediatR (licença comercial), sem banco de leitura separado, sem event sourcing.

## Consequências
Leituras evoluem (índices, SQL) sem tocar o domínio; escrita mantém invariantes. Custo: dois caminhos de acesso a dados para entender.
