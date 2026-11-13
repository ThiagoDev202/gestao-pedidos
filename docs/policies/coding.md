# Política: código

Vale para qualquer agente ou pessoa que escreva código aqui. Leia **antes** de implementar.

## Antes de codar
- Confirme no código o que a spec afirma sobre o sistema atual (Princípio I). Doc não é prova.
- Declare premissas. Mais de uma interpretação: apresente as opções e pergunte; não escolha em silêncio.
- Existe um caminho mais simples? Diga.

## Durante
- **Mudança mínima**: cada linha rastreia até um requisito da spec ou à causa do bug.
- **Cirúrgica**: não reformate, não renomeie, não "melhore" código vizinho.
- **Fora do escopo**: registre em `docs/known-issues.md`; não corrija de carona.
- Sem abstração, flag ou configuração que ninguém pediu. Dependência nova só com justificativa no plano.

## Clean Architecture e SOLID
- `Domain` ← `Application` ← `Infrastructure`/`Api`. Teste de arquitetura impõe a regra (`docs/patterns/clean-architecture.md`).
- Um handler por caso de uso (SRP); ports na `Application`, adapters na `Infrastructure` (DIP); repositórios por agregado e queries de leitura separadas (ISP).
- Padrão GoF só com problema real nomeado no plano (`docs/patterns/solid-gof.md`).

## C#
- `Nullable` habilitado e warnings como erro; sem `!` (null-forgiving) para silenciar.
- Domínio em pt-BR, sufixos técnicos em inglês. `record` para requests/responses; entidades com construtor privado e fábrica (`Pedido.Criar`).
- Sem `DateTime.UtcNow` direto: use `IClock`.
- Nunca concatene SQL. Sempre parâmetros.

## TypeScript
- `strict`; sem `any`; `null` tratado. Named exports em utilitários e hooks; default export em páginas.
- HTTP só em `api/`.

## Comentários
- **Não escreva comentários**: de linha, bloco, XML-doc ou JSDoc, em nenhum arquivo de código, script ou workflow. Nomes explicam o código; o porquê vai no commit, no PR ou na spec. Exceção: diretivas de ferramenta (`eslint-disable`, `@ts-expect-error`, `#pragma`, `SuppressMessage`). O guardrail barra.

## Depois de cada mudança
- "Um engenheiro sênior diria que está complicado demais?" Se sim, simplifique.
- Rode os gates da camada alterada (`AGENTS.md`, Comandos).
