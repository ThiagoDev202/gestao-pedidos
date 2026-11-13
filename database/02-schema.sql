USE GestaoPedidos;
GO

IF OBJECT_ID(N'dbo.Clientes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Clientes (
        Id INT IDENTITY(1,1) NOT NULL,
        Nome NVARCHAR(200) NOT NULL,
        Email NVARCHAR(254) NOT NULL,
        CriadoEm DATETIME2(3) NOT NULL CONSTRAINT DF_Clientes_CriadoEm DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_Clientes PRIMARY KEY (Id),
        CONSTRAINT UQ_Clientes_Email UNIQUE (Email)
    );
END
GO

IF OBJECT_ID(N'dbo.Produtos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Produtos (
        Id INT IDENTITY(1,1) NOT NULL,
        Nome NVARCHAR(200) NOT NULL,
        Sku NVARCHAR(50) NOT NULL,
        Preco DECIMAL(18,2) NOT NULL,
        Estoque INT NOT NULL,
        CONSTRAINT PK_Produtos PRIMARY KEY (Id),
        CONSTRAINT UQ_Produtos_Sku UNIQUE (Sku),
        CONSTRAINT CK_Produtos_Preco CHECK (Preco >= 0),
        CONSTRAINT CK_Produtos_Estoque CHECK (Estoque >= 0)
    );
END
GO

IF OBJECT_ID(N'dbo.Pedidos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Pedidos (
        Id INT IDENTITY(1,1) NOT NULL,
        ClienteId INT NOT NULL,
        DataPedido DATETIME2(3) NOT NULL,
        ValorTotal DECIMAL(18,2) NOT NULL,
        CONSTRAINT PK_Pedidos PRIMARY KEY (Id),
        CONSTRAINT FK_Pedidos_Clientes FOREIGN KEY (ClienteId) REFERENCES dbo.Clientes (Id),
        CONSTRAINT CK_Pedidos_ValorTotal CHECK (ValorTotal >= 0)
    );
END
GO

IF OBJECT_ID(N'dbo.ItensPedido', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ItensPedido (
        Id INT IDENTITY(1,1) NOT NULL,
        PedidoId INT NOT NULL,
        ProdutoId INT NOT NULL,
        Quantidade INT NOT NULL,
        PrecoUnitario DECIMAL(18,2) NOT NULL,
        CONSTRAINT PK_ItensPedido PRIMARY KEY (Id),
        CONSTRAINT FK_ItensPedido_Pedidos FOREIGN KEY (PedidoId) REFERENCES dbo.Pedidos (Id) ON DELETE CASCADE,
        CONSTRAINT FK_ItensPedido_Produtos FOREIGN KEY (ProdutoId) REFERENCES dbo.Produtos (Id),
        CONSTRAINT UQ_ItensPedido_PedidoId_ProdutoId UNIQUE (PedidoId, ProdutoId),
        CONSTRAINT CK_ItensPedido_Quantidade CHECK (Quantidade > 0),
        CONSTRAINT CK_ItensPedido_PrecoUnitario CHECK (PrecoUnitario >= 0)
    );
END
GO

IF OBJECT_ID(N'dbo.IdempotenciaPedidos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.IdempotenciaPedidos (
        Chave UNIQUEIDENTIFIER NOT NULL,
        HashCorpo CHAR(64) NOT NULL,
        PedidoId INT NOT NULL,
        CriadoEm DATETIME2(3) NOT NULL CONSTRAINT DF_IdempotenciaPedidos_CriadoEm DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_IdempotenciaPedidos PRIMARY KEY (Chave),
        CONSTRAINT FK_IdempotenciaPedidos_Pedidos FOREIGN KEY (PedidoId) REFERENCES dbo.Pedidos (Id) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Pedidos_ClienteId_DataPedido' AND object_id = OBJECT_ID(N'dbo.Pedidos'))
    CREATE INDEX IX_Pedidos_ClienteId_DataPedido ON dbo.Pedidos (ClienteId, DataPedido) INCLUDE (ValorTotal);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Pedidos_DataPedido' AND object_id = OBJECT_ID(N'dbo.Pedidos'))
    CREATE INDEX IX_Pedidos_DataPedido ON dbo.Pedidos (DataPedido) INCLUDE (ClienteId, ValorTotal);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ItensPedido_PedidoId' AND object_id = OBJECT_ID(N'dbo.ItensPedido'))
    CREATE INDEX IX_ItensPedido_PedidoId ON dbo.ItensPedido (PedidoId) INCLUDE (ProdutoId, Quantidade, PrecoUnitario);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ItensPedido_ProdutoId' AND object_id = OBJECT_ID(N'dbo.ItensPedido'))
    CREATE INDEX IX_ItensPedido_ProdutoId ON dbo.ItensPedido (ProdutoId);
GO
