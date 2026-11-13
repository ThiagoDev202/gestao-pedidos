# Política: testes

- **Só o `qa-guardian` escreve, altera e aprova testes**, a partir dos requisitos e **antes** da implementação. Quem implementa faz o código passar e não toca nos testes. `scripts/qa-seal.sh` lacra a fase RED por hash e reprova teste alterado, fora do lacre ou apagado.
- **Pirâmide**:
  1. Unitários (xUnit + Shouldly + NSubstitute): domínio, handlers, validators. Rápidos, sem I/O.
  2. Arquitetura (NetArchTest): regra de dependência entre camadas.
  3. Integração: `WebApplicationFactory` + **SQL Server real via Testcontainers** + Respawn; aplica `database/*.sql`; cobre endpoints, ProblemDetails, transação, concorrência e rate limiting.
  4. Front (Vitest + Testing Library): validações, estados, chamadas de `api/` mockadas.
  5. e2e (Playwright): fluxo criar pedido → ver na listagem, contra o `docker compose`.
- **Nunca** enfraquecer, apagar, pular (`Skip`, `it.skip`) ou desativar teste para passar.
- **Nomes** em inglês, descrevendo comportamento. **Sem comentários** nos testes.
- **Dados**: builders de teste; relógio por `IClock` fake; um teste não depende de outro; banco limpo entre testes.
- **Estabilidade**: rodar a suíte também com `TZ=Pacific/Kiritimati`; teste que só passa em um fuso é bug (do teste ou do código).
- **CI**: backend (unit + arquitetura + integração), frontend (lint, typecheck, Vitest, build) e e2e (stack completa) em todo PR que toca a área.
- Cobertura é meio, não fim: regra de negócio nova sem teste é REPROVADO pelo `qa-guardian`.
