# Política: banco de dados

- **Fonte do schema**: `database/*.sql`. O EF Core **não** cria nem migra tabelas; ele só mapeia o que os scripts criam. Mudou o schema? Mude o script e o mapeamento no mesmo PR.
- **Scripts numerados e idempotentes**: `01-create-database.sql`, `02-schema.sql`, `03-seed.sql`; reexecutar não pode falhar nem duplicar dados (`IF NOT EXISTS`, `WHERE NOT EXISTS`). Executados no `docker compose up` pelo serviço `db-init`.
- **Integridade no banco**: PK em toda tabela, FKs explícitas, `UNIQUE` onde o domínio exige (`Clientes.Email`, `Produtos.Sku`, `ItensPedido(PedidoId, ProdutoId)`), `CHECK` para invariantes (`Preco >= 0`, `Estoque >= 0`, `Quantidade > 0`). O banco é a última linha de defesa.
- **Tipos**: dinheiro em `decimal(18,2)`; datas em `datetime2` gravadas em UTC.
- **Índices**: um por consulta real (filtro, join, ordenação), com `INCLUDE` para evitar lookups. Todo índice tem justificativa com plano de execução (`/sql-investigate`) e custo anotado (escrita, espaço). Sem índice "por via das dúvidas".
- **Consultas**: predicado sargável (sem função na coluna), intervalo semiaberto para datas, `ORDER BY` determinístico, paginação com `OFFSET/FETCH`.
- **Seed**: volume suficiente para o plano de execução importar (dezenas de milhares de pedidos), gerado por script set-based e determinístico.
- Nunca editar o banco à mão; nunca `DROP` em script versionado sem ADR.
