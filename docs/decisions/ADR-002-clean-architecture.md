# ADR-002 — Clean Architecture em quatro projetos

**Status:** Aceito · **Data:** 2026-11-13

## Contexto
O enunciado pede clean code, Clean Architecture, SOLID e padrões GoF. A regra de negócio do pedido (estoque, itens, total) deve ser testável sem banco.

## Decisão
Quatro projetos: `Domain` (zero dependências) ← `Application` (casos de uso, ports) ← `Infrastructure` (EF Core, Dapper, adapters) e `Api` (Minimal APIs, DI). Um projeto de teste por nível: unit, arquitetura (NetArchTest) e integração. A regra de dependência é imposta por teste, não por convenção.

## Alternativas
- Projeto único com pastas: mais simples, mas a regra de dependência fica só no papel.
- Vertical slices puras: boas para CRUD, mas o enunciado pede camadas explícitas.

## Consequências
Mais arquivos por caso de uso; ganho em testabilidade e em explicar o código no vídeo.
