USE GestaoPedidos;
GO

SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM dbo.Clientes)
BEGIN
    DECLARE @Nomes TABLE (Idx INT PRIMARY KEY, Nome NVARCHAR(50));
    INSERT INTO @Nomes (Idx, Nome) VALUES
        (0, N'Ana'), (1, N'Bruno'), (2, N'Carla'), (3, N'Diego'), (4, N'Eduarda'),
        (5, N'Felipe'), (6, N'Gabriela'), (7, N'Henrique'), (8, N'Isabela'), (9, N'João'),
        (10, N'Karen'), (11, N'Lucas'), (12, N'Marina'), (13, N'Nicolas'), (14, N'Olivia'),
        (15, N'Paulo'), (16, N'Rafaela'), (17, N'Samuel'), (18, N'Tatiana'), (19, N'Vinícius');

    DECLARE @Sobrenomes TABLE (Idx INT PRIMARY KEY, Nome NVARCHAR(50));
    INSERT INTO @Sobrenomes (Idx, Nome) VALUES
        (0, N'Silva'), (1, N'Souza'), (2, N'Oliveira'), (3, N'Santos'),
        (4, N'Pereira'), (5, N'Lima'), (6, N'Costa'), (7, N'Ribeiro');

    INSERT INTO dbo.Clientes (Nome, Email)
    SELECT
        n.Nome + N' ' + s.Nome,
        LOWER(n.Nome + N'.' + s.Nome) + N'.' + CAST(g.value AS NVARCHAR(10)) + N'@exemplo.com'
    FROM GENERATE_SERIES(1, 200) AS g
    JOIN @Nomes AS n ON n.Idx = g.value % 20
    JOIN @Sobrenomes AS s ON s.Idx = (g.value / 20) % 8
    ORDER BY g.value;
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Produtos)
BEGIN
    INSERT INTO dbo.Produtos (Nome, Sku, Preco, Estoque)
    SELECT
        CASE WHEN g.value = 1 THEN N'Produto de Estoque Unitário' ELSE N'Produto ' + CAST(g.value AS NVARCHAR(10)) END,
        N'SKU-' + RIGHT(N'0000' + CAST(g.value AS NVARCHAR(10)), 4),
        CAST(10 + (g.value * 7) % 490 AS DECIMAL(18,2)) + 0.90,
        CASE WHEN g.value = 1 THEN 1 WHEN g.value = 2 THEN 5 ELSE 50 + (g.value * 13) % 450 END
    FROM GENERATE_SERIES(1, 100) AS g
    ORDER BY g.value;
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Pedidos)
BEGIN
    SET IDENTITY_INSERT dbo.Pedidos ON;
    INSERT INTO dbo.Pedidos (Id, ClienteId, DataPedido, ValorTotal)
    SELECT
        g.value,
        1 + (g.value * 37) % 200,
        DATEADD(MINUTE, (g.value * 7919) % (60 * 24 * 730), CAST('2025-01-01' AS DATETIME2(3))),
        0
    FROM GENERATE_SERIES(1, 50000) AS g
    ORDER BY g.value;
    SET IDENTITY_INSERT dbo.Pedidos OFF;

    INSERT INTO dbo.ItensPedido (PedidoId, ProdutoId, Quantidade, PrecoUnitario)
    SELECT
        p.Id,
        1 + (p.Id * 13 + j.value * 29) % 100,
        1 + (p.Id + j.value) % 5,
        pr.Preco
    FROM dbo.Pedidos AS p
    CROSS APPLY GENERATE_SERIES(0, 4) AS j
    JOIN dbo.Produtos AS pr ON pr.Id = 1 + (p.Id * 13 + j.value * 29) % 100
    WHERE j.value < 1 + p.Id % 5;

    UPDATE p
    SET ValorTotal = t.Total
    FROM dbo.Pedidos AS p
    JOIN (
        SELECT PedidoId, SUM(Quantidade * PrecoUnitario) AS Total
        FROM dbo.ItensPedido
        GROUP BY PedidoId
    ) AS t ON t.PedidoId = p.Id;
END
GO
