# Especificação da Feature: Mini Gestão de Pedidos

**Branch**: `docs/001-sdd-gestao-pedidos`
**Criada em**: 2026-11-13
**Status**: Rascunho
**Entrada**: Aplicação de cadastro e consulta de pedidos de clientes, com criação validada de pedidos, listagem filtrada e paginada e relatório dos melhores clientes.

## Cenários de Usuário e Testes *(obrigatório)*

### User Story 1 - Criar pedido com itens (Prioridade: P1)

O Operador registra um pedido para um cliente existente, informando os produtos e as quantidades. O sistema só aceita o pedido se todos os itens puderem ser atendidos pelo estoque; ao aceitar, registra o pedido com o total calculado e reduz o estoque dos produtos.

**Por que esta prioridade**: é o núcleo do negócio; sem criar pedidos não há nada a consultar nem a reportar.

**Teste independente**: criar um pedido válido e verificar o pedido registrado (itens, preço, total, data) e o estoque reduzido; tentar pedidos inválidos e verificar que nada muda.

**Cenários de aceite**:

1. **Dado** um cliente existente e produtos com estoque suficiente, **Quando** o Operador cria um pedido com esses itens, **Então** o pedido é registrado com os itens, o preço de cada item igual ao preço atual do produto, o valor total igual à soma de quantidade × preço de todos os itens, a data/hora do registro definida pelo sistema, e o estoque de cada produto reduzido na quantidade pedida.
2. **Dado** um cliente que não existe, **Quando** o Operador tenta criar o pedido, **Então** o pedido é recusado com erro de regra de negócio que identifica o cliente como inexistente, e nada é gravado.
3. **Dado** um pedido sem nenhum item, **Quando** o Operador tenta criá-lo, **Então** o pedido é recusado com erro de validação.
4. **Dado** um item com quantidade zero ou negativa, **Quando** o Operador tenta criar o pedido, **Então** o pedido é recusado com erro de validação que aponta o item.
5. **Dado** um item cujo produto não existe, **Quando** o Operador tenta criar o pedido, **Então** o pedido é recusado com erro de regra de negócio que identifica o produto, e nada é gravado.
6. **Dado** um pedido em que ao menos um item pede mais do que o estoque disponível, **Quando** o Operador tenta criá-lo, **Então** o pedido é recusado com erro de regra de negócio que identifica o produto e o estoque disponível, e **nenhum** produto do pedido tem o estoque alterado.
7. **Dado** um produto com apenas 1 unidade em estoque, **Quando** dois pedidos simultâneos pedem essa unidade, **Então** exatamente um é aceito, o outro é recusado por estoque insuficiente, e o estoque final é 0 (nunca negativo).
8. **Dado** um produto cujo preço é alterado depois de um pedido, **Quando** o pedido antigo é consultado, **Então** o preço do item e o total permanecem os da data da criação.
9. **Dado** um pedido enviado com uma chave de idempotência, **Quando** a mesma requisição é reenviada com a mesma chave (duplo clique, nova tentativa após falha de rede), **Então** nenhum pedido novo é criado, o estoque não é baixado de novo e o sistema devolve o pedido já criado.
10. **Dado** uma chave de idempotência já usada, **Quando** ela chega com itens ou cliente diferentes, **Então** o pedido é recusado com erro de regra de negócio que indica o reúso indevido da chave.
11. **Dado** dois pedidos idênticos enviados com chaves diferentes (ou sem chave), **Quando** ambos são válidos, **Então** os dois são criados, pois comprar o mesmo conteúdo duas vezes é permitido.

---

### User Story 2 - Listar pedidos com filtros e paginação (Prioridade: P1)

O Operador consulta os pedidos numa tela de listagem, filtrando por cliente e por período e navegando por páginas.

**Por que esta prioridade**: sem consulta, os pedidos criados não servem ao negócio; junto com a US1 forma o MVP.

**Teste independente**: com pedidos de exemplo carregados, filtrar por cliente, por período e por ambos, e paginar; conferir totais e ordem.

**Cenários de aceite**:

1. **Dado** pedidos de vários clientes, **Quando** o Operador abre a listagem sem filtros, **Então** vê a primeira página dos pedidos do mais recente para o mais antigo, com total de pedidos e total de páginas.
2. **Dado** pedidos de vários clientes, **Quando** filtra por um cliente, **Então** vê somente os pedidos desse cliente.
3. **Dado** pedidos em datas diversas, **Quando** filtra por período com data inicial e final, **Então** vê os pedidos cuja data cai entre o início do dia inicial e o fim do dia final, inclusive os dois dias.
4. **Dado** mais pedidos que o tamanho da página, **Quando** o Operador muda de página, **Então** vê os pedidos seguintes sem repetir nem omitir nenhum.
5. **Dado** uma página além da última, **Quando** o Operador a solicita, **Então** vê lista vazia com os totais corretos.
6. **Dado** parâmetros inválidos (página menor que 1, tamanho fora de 1 a 100, data inicial depois da final), **Quando** o Operador consulta, **Então** recebe erro de validação.
7. **Dado** nenhum pedido que atenda aos filtros, **Quando** o Operador consulta, **Então** a tela mostra o estado vazio com opção de limpar os filtros.

---

### User Story 3 - Relatório dos 5 melhores clientes (Prioridade: P2)

O Gestor informa um período e vê os 5 clientes com maior valor total em pedidos nesse período.

**Por que esta prioridade**: entrega valor gerencial, mas depende de haver pedidos (US1 e carga de exemplo).

**Teste independente**: com pedidos de exemplo conhecidos, pedir o relatório de um período e comparar com o ranking esperado calculado à mão.

**Cenários de aceite**:

1. **Dado** pedidos de mais de 5 clientes no período, **Quando** o Gestor pede o relatório, **Então** recebe exatamente 5 clientes ordenados do maior para o menor valor total, cada um com nome e valor total.
2. **Dado** dois clientes diferentes com o mesmo nome, **Quando** o relatório é gerado, **Então** eles aparecem como entradas separadas, cada um com o seu próprio total.
3. **Dado** clientes com o mesmo valor total, **Quando** o relatório é gerado, **Então** o desempate é por nome em ordem alfabética, de forma estável.
4. **Dado** clientes sem pedidos no período, **Quando** o relatório é gerado, **Então** eles não aparecem.
5. **Dado** menos de 5 clientes com pedidos no período, **Quando** o relatório é gerado, **Então** aparecem só os que têm pedidos.
6. **Dado** período ausente ou inválido, **Quando** o Gestor pede o relatório, **Então** recebe erro de validação.

---

### User Story 4 - Criar pedido pela tela (Prioridade: P2)

Na tela de listagem, o Operador abre um formulário de novo pedido, escolhe o cliente, adiciona itens (produto e quantidade) e confirma. Erros de negócio aparecem de forma clara e o pedido criado surge na listagem.

**Por que esta prioridade**: expõe a US1 a quem não usa a interface de programação diretamente.

**Teste independente**: pela interface, criar um pedido e vê-lo na listagem; provocar erro de estoque e ver a mensagem.

**Cenários de aceite**:

1. **Dado** o formulário aberto, **Quando** o Operador escolhe um cliente, adiciona itens e confirma, **Então** o formulário fecha, a listagem é atualizada e o novo pedido aparece.
2. **Dado** o formulário aberto, **Quando** o Operador adiciona um item, **Então** vê o estoque disponível do produto e o subtotal do item, e o total do pedido atualizado.
3. **Dado** campos obrigatórios vazios ou quantidade inválida, **Quando** o Operador tenta confirmar, **Então** vê a mensagem de erro junto do campo e nada é enviado.
4. **Dado** um pedido recusado por estoque insuficiente, **Quando** o sistema responde, **Então** o formulário permanece aberto com os dados preenchidos e mostra a mensagem do problema.
5. **Dado** o envio em andamento, **Quando** o Operador tenta confirmar de novo, **Então** o botão está desabilitado e nenhum pedido duplicado é criado.

---

### User Story 5 - Consultas de apoio e detalhe do pedido (Prioridade: P3)

O Operador vê a lista de clientes e a lista de produtos (com preço e estoque) para montar um pedido, e abre o detalhe de um pedido com todos os seus itens.

**Por que esta prioridade**: necessária para a US4 funcionar com dados reais, mas simples e de baixo risco.

**Teste independente**: consultar as listas e o detalhe de um pedido de exemplo.

**Cenários de aceite**:

1. **Dado** clientes cadastrados, **Quando** o Operador consulta clientes, **Então** recebe nome e identificador de cada um, em ordem alfabética.
2. **Dado** produtos cadastrados, **Quando** o Operador consulta produtos, **Então** recebe nome, código (SKU), preço e estoque atual de cada um.
3. **Dado** um pedido existente, **Quando** o Operador consulta seu detalhe, **Então** vê cliente, data, itens (produto, quantidade, preço, subtotal) e total.
4. **Dado** um pedido que não existe, **Quando** o Operador consulta seu detalhe, **Então** recebe erro de recurso não encontrado.

---

### Casos de Borda

- Quantidade muito grande, que ultrapassa o estoque ou o limite de itens por pedido: recusada com mensagem clara.
- O mesmo produto aparecendo em mais de uma linha do pedido: o pedido é recusado com erro de validação que identifica o produto repetido (decisão de 2026-11-13).
- Pedido com mais de 50 itens: recusado com erro de validação (decisão de 2026-11-13).
- Falha temporária no armazenamento no meio da criação do pedido: nada fica gravado pela metade (tudo ou nada).
- Duas requisições simultâneas com a mesma chave de idempotência: exatamente um pedido é criado e as duas respostas apontam para ele.
- Excesso de criações de pedido em pouco tempo pelo mesmo cliente de rede: o sistema recusa o excedente indicando quando tentar de novo.
- Filtros com a mesma data inicial e final: retornam os pedidos daquele dia inteiro.
- Datas de pedido e de filtro tratadas de forma consistente entre fusos: o registro é em UTC e a tela exibe no fuso local.

## Requisitos *(obrigatório)*

### Requisitos Funcionais

- **FR-001**: O sistema DEVE permitir criar um pedido para um cliente existente com um ou mais itens (produto e quantidade).
- **FR-002**: O sistema DEVE recusar o pedido quando o cliente não existe, com erro de regra de negócio.
- **FR-003**: O sistema DEVE recusar com erro de validação o pedido sem itens, com mais de 50 itens, com o mesmo produto em mais de uma linha, ou com item de quantidade menor ou igual a zero.
- **FR-004**: O sistema DEVE recusar o pedido quando algum produto não existe ou não tem estoque suficiente, com erro de regra de negócio que identifica o produto.
- **FR-005**: A criação do pedido DEVE ser atômica: ou todos os itens são aceitos e o estoque de todos é reduzido, ou nada é gravado.
- **FR-006**: O sistema DEVE obter o preço unitário de cada item do cadastro do produto no momento da criação; o solicitante não informa preço.
- **FR-007**: O sistema DEVE congelar o preço unitário no item e calcular o valor total do pedido como a soma de quantidade × preço unitário dos itens, guardando-o com o pedido.
- **FR-008**: O sistema DEVE definir a data/hora do pedido no momento da criação, em UTC.
- **FR-009**: O sistema DEVE garantir que o estoque nunca fique negativo, mesmo com pedidos simultâneos pelo mesmo produto.
- **FR-010**: O sistema DEVE listar pedidos com filtro opcional por cliente e por período (datas inclusivas), ordenados do mais recente para o mais antigo.
- **FR-011**: A listagem DEVE ser paginada com página a partir de 1 e tamanho de 1 a 100, informando total de pedidos e total de páginas.
- **FR-012**: O sistema DEVE retornar o relatório dos 5 clientes com maior valor total de pedidos no período informado, agrupando por cliente (não por nome), do maior para o menor valor, com desempate por nome.
- **FR-013**: O relatório DEVE exigir o período e recusar período ausente ou invertido.
- **FR-014**: O sistema DEVE oferecer consulta de clientes, de produtos (com preço e estoque) e do detalhe de um pedido.
- **FR-015**: Todas as respostas de erro DEVEM seguir o mesmo formato padronizado, com um código estável e específico para cada situação (nunca uma mensagem genérica quando existe causa conhecida), distinguindo validação, regra de negócio, recurso não encontrado, limite de taxa e falha interna.
- **FR-023**: Erros de validação DEVEM listar todos os problemas de uma vez, cada um indicando o campo afetado.
- **FR-024**: A interface DEVE mostrar, para cada código de erro, uma mensagem específica em português que diga o que aconteceu e o que o usuário pode fazer (por exemplo, produto e quantidade disponível no erro de estoque), colocar erros de campo junto do campo, e, em falhas inesperadas, exibir um código de suporte.
- **FR-016**: O sistema DEVE limitar a taxa de criação de pedidos por origem de rede, recusando o excedente com indicação de quando tentar novamente.
- **FR-017**: O sistema DEVE recuperar-se automaticamente de falhas transitórias do armazenamento sem duplicar pedidos nem baixar o estoque duas vezes.
- **FR-018**: A interface DEVE oferecer listagem de pedidos com filtros por cliente e período, paginação e estados de vazio, carregamento e erro.
- **FR-019**: A interface DEVE oferecer criação de pedido por formulário com validação, exibição das mensagens de erro de negócio e atualização da listagem após o sucesso.
- **FR-020**: O ambiente completo (banco com estrutura e dados de exemplo, serviço e interface) DEVE subir com um único comando, sem passos manuais.
- **FR-022**: O sistema DEVE aceitar uma chave de idempotência opcional na criação do pedido: a mesma chave com o mesmo conteúdo devolve o pedido original sem novo efeito; a mesma chave com conteúdo diferente é recusada. A interface DEVE enviar uma chave nova a cada tentativa de envio do formulário.
- **FR-021**: O sistema DEVE manter a integridade dos dados no próprio armazenamento: identificadores únicos, e-mail de cliente único, código de produto único, um produto no máximo uma vez por pedido, preço e estoque não negativos e quantidade positiva.

### Entidades-chave

- **Cliente**: quem compra. Tem nome e e-mail único.
- **Produto**: item vendável. Tem nome, código (SKU) único, preço atual e estoque.
- **Pedido**: compra de um cliente. Tem cliente, data/hora de criação (UTC) e valor total.
- **Registro de idempotência**: associa uma chave enviada pelo solicitante ao pedido criado e ao conteúdo original, para reconhecer reenvios.
- **Item do pedido**: linha de um pedido. Tem produto, quantidade e preço unitário congelado; um produto aparece no máximo uma vez por pedido.

## Critérios de Sucesso *(obrigatório)*

### Resultados Mensuráveis

- **SC-001**: Um Operador cria um pedido de 3 itens pela interface em menos de 1 minuto, sem treinamento.
- **SC-002**: 100% das tentativas de pedido inválido (cliente inexistente, sem itens, quantidade inválida, estoque insuficiente) são recusadas sem alterar nenhum estoque.
- **SC-003**: Em teste com 50 pedidos simultâneos pelo último item de um produto, exatamente 1 é aceito e o estoque final é 0.
- **SC-009**: 100% dos códigos de erro do catálogo têm teste na API e mensagem específica na interface; nenhuma tela exibe texto técnico cru de erro.
- **SC-008**: Em teste com 10 envios simultâneos da mesma requisição com a mesma chave de idempotência, exatamente 1 pedido é criado e o estoque é baixado uma única vez.
- **SC-004**: Com a carga de exemplo (dezenas de milhares de pedidos), a primeira página da listagem e o relatório de melhores clientes aparecem em menos de 1 segundo para o usuário.
- **SC-005**: O ranking dos 5 melhores clientes coincide em 100% com o cálculo manual sobre os dados de exemplo, inclusive com clientes homônimos e empates.
- **SC-006**: Um avaliador sobe o ambiente completo com um único comando e executa o fluxo de criar e consultar um pedido em menos de 5 minutos.
- **SC-007**: Toda regra de negócio acima tem ao menos um teste automatizado, e o fluxo "criar pedido e vê-lo na listagem" tem teste de ponta a ponta.

## Premissas

- Autenticação e autorização estão fora do escopo; Operador e Gestor usam a mesma interface sem login.
- Clientes e produtos são cadastrados pela carga de exemplo; não há cadastro nem edição deles pela interface nesta versão.
- Pedidos não são editados nem cancelados depois de criados; não há devolução de estoque.
- Uma única moeda (real brasileiro) e um único estoque por produto.
- Datas são registradas em UTC; filtros por data usam o dia inteiro, e a interface exibe no fuso do usuário.
- Idioma da interface e das mensagens de erro: português do Brasil.
- Detalhes de arquitetura e tecnologia (linguagens, banco, bibliotecas, resiliência) ficam no plano de implementação, conforme a constitution.
