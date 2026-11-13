# Política: padrão de erros (API e interface)

Todo erro da API tem o **mesmo formato** (ProblemDetails, RFC 9457) e um **código estável e específico**. Nunca um erro genérico quando existe um específico. A interface traduz cada código em uma mensagem clara em pt-BR.

## Formato único da API

`Content-Type: application/problem+json`

```json
{
  "type": "urn:gestaopedidos:erro:estoque_insuficiente",
  "title": "Estoque insuficiente",
  "status": 422,
  "detail": "O produto Teclado Mecânico tem 2 unidades em estoque e foram pedidas 5.",
  "code": "estoque_insuficiente",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00",
  "meta": { "produtoId": 12, "produtoNome": "Teclado Mecânico", "solicitado": 5, "disponivel": 2 },
  "errors": []
}
```

| Campo | Regra |
|---|---|
| `type` | `urn:gestaopedidos:erro:<code>` |
| `title` | curto, pt-BR, igual para o mesmo `code` |
| `status` | HTTP coerente com a categoria abaixo |
| `detail` | frase pt-BR específica daquela ocorrência (sem SQL, stack trace ou nomes internos) |
| `code` | identificador estável em `snake_case`; **é o contrato com o front** |
| `traceId` | correlação com os logs; sempre presente |
| `meta` | dados estruturados da ocorrência (ids, quantidades) para a interface montar a mensagem; opcional |
| `errors` | só no 400: lista de `{ campo, code, mensagem }`, **uma entrada por problema** (todos de uma vez, não só o primeiro) |

Erro de validação com vários problemas devolve todos juntos: o usuário corrige tudo numa tentativa.

## Categorias e catálogo

| Status | Quando | Códigos |
|---|---|---|
| 400 | dados malformados ou fora dos limites (`code` raiz `validacao_falhou`; cada item de `errors` tem o código do campo) | `corpo_invalido`, `cliente_id_invalido`, `itens_vazio`, `itens_excedem_limite`, `produto_id_invalido`, `quantidade_invalida`, `produto_repetido`, `chave_idempotencia_invalida`, `pagina_invalida`, `tamanho_pagina_invalido`, `data_invalida`, `periodo_obrigatorio`, `periodo_invertido` |
| 404 | recurso inexistente | `pedido_nao_encontrado`, `recurso_nao_encontrado` |
| 415 | corpo que não é JSON | `formato_nao_suportado` |
| 422 | dados válidos que violam regra de negócio | `cliente_inexistente`, `produto_inexistente`, `estoque_insuficiente`, `chave_reutilizada` |
| 429 | limite de taxa | `limite_requisicoes_excedido` (header `Retry-After`; `meta.retryAfterSegundos`) |
| 500 | falha inesperada | `erro_interno` (detalhe genérico; causa só no log, ligada ao `traceId`) |
| 503 | banco indisponível após os retries | `banco_indisponivel` |

`meta` por código: `cliente_inexistente` → `clienteId`; `produto_inexistente` → `produtoId`; `estoque_insuficiente` → `produtoId`, `produtoNome`, `solicitado`, `disponivel` (o primeiro item sem estoque, em ordem de `ProdutoId`); `pedido_nao_encontrado` → `pedidoId`; `itens_excedem_limite` → `maximo`; `limite_requisicoes_excedido` → `retryAfterSegundos`.

## Regras para o backend
- Um catálogo único (`backend/src/GestaoPedidos.Api/Errors/ErrorCatalog.cs`) mapeia exceção de domínio ou falha de validação → `status`, `title`, `code`, `detail`, `meta`. Endpoint e handler **não** montam ProblemDetails à mão.
- Todo erro do domínio é uma exceção tipada própria (`EstoqueInsuficienteException`, …), nunca `Exception` genérica nem string.
- Um único `IExceptionHandler` e o `StatusCodePages` convertem tudo (inclusive 404 de rota, 415, 429 e erro de binding) para este formato.
- Novo código de erro = entrada no catálogo + `contracts/openapi.yaml` + este doc + teste de integração que confere `status`, `code` e `meta`.
- Nunca vazar SQL, stack trace, nome de tabela ou de exceção em `detail`.

## Regras para o frontend
- **Um único ponto de entrada**: `frontend/src/shared/api/http-client.ts` transforma toda resposta de erro em `ApiError` (`status`, `code`, `title`, `detail`, `meta`, `errors`, `traceId`). Falha de rede vira `ApiError` com `code: sem_conexao`; resposta que não é ProblemDetails vira `resposta_invalida`.
- **Um único dicionário** (`frontend/src/shared/api/error-messages.ts`) mapeia `code` → mensagem pt-BR, interpolando `meta` (ex.: "Só há {disponivel} unidade(s) de {produtoNome}; você pediu {solicitado}."). Código desconhecido cai na mensagem do `status`, nunca em texto técnico cru.
- **Onde aparece**: erros de campo (400) junto do campo do formulário (`campo` → caminho do react-hook-form, ex.: `itens.0.quantidade`); erros de regra (422) no alerta do formulário, que **permanece aberto com os dados**; 404, 429, 500, 503 e rede em alerta/toast com ação ("Tentar de novo"; no 429 com a espera de `Retry-After`).
- **Erro inesperado** (500 e desconhecidos): mensagem amigável e o `traceId` como "código de suporte" para o usuário informar.
- Componentes únicos (`ErrorAlert`, `FieldError`) usados por todas as telas; proibido `alert()`, `console.error` como feedback ou mensagem escrita à mão numa tela.
- Todo `code` do catálogo tem entrada no dicionário; um teste Vitest falha se faltar (o `qa-guardian` mantém a lista alinhada a este doc).

## Testes (qa-guardian)
- Integração: cada `code` do catálogo tem ao menos um teste que confere `status`, `code`, `type`, `traceId` e `meta`; 400 com vários problemas devolve todos.
- Unitário: o catálogo cobre todas as exceções de domínio; handler de exceção nunca expõe a mensagem interna.
- Front: cada `code` renderiza a mensagem esperada; erro de campo cai no campo certo; 500 mostra o `traceId`.
