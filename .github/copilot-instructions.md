# Instruções do projeto

As regras completas estão em [`AGENTS.md`](../AGENTS.md) (fonte única) e o índice dos docs em [`CLAUDE.md`](../CLAUDE.md). Leia antes de sugerir código.

- O código é a fonte da verdade: confira no código antes de afirmar.
- Feature, mudança de API ou de schema começa por spec (`specs/`).
- Testes nascem dos requisitos, antes do código; quem implementa não altera testes.
- Sem comentários no código, sem `any` novo, sem URL ou segredo fixos (use variável de ambiente).
- Domínio em pt-BR, termos técnicos e commits em inglês.
- Clean Architecture: Domain ← Application ← Infrastructure/Api, verificada por teste de arquitetura.
- Conventional Commits com escopo (`feat(api): …`), commits só no nome de quem faz.
