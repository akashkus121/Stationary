-- Table-Valued Parameter Type for Bulk Stock Updates
-- This allows efficient bulk operations without multiple round trips

CREATE TYPE [dbo].[StockUpdateTableType] AS TABLE
(
    [ProductId] INT NOT NULL,
    [NewStockQuantity] INT NOT NULL,
    [NewLowStockThreshold] INT NOT NULL,
    [ProductName] NVARCHAR(100) NULL,
    [CurrentStock] INT NULL,
    [CurrentLowStockThreshold] INT NULL
);
GO