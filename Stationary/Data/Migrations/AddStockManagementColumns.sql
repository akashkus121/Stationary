-- Migration: Add Stock Management Columns
-- Run this script to add new columns for stock management features

-- Add IsVisible column to Products table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND name = 'IsVisible')
BEGIN
    ALTER TABLE [dbo].[Products] ADD [IsVisible] BIT NOT NULL DEFAULT 1;
    PRINT 'Added IsVisible column to Products table';
END

-- Add LowStockThreshold column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND name = 'LowStockThreshold')
BEGIN
    ALTER TABLE [dbo].[Products] ADD [LowStockThreshold] INT NOT NULL DEFAULT 10;
    PRINT 'Added LowStockThreshold column to Products table';
END

-- Update existing products to have default values
UPDATE [dbo].[Products] 
SET [IsVisible] = 1, [LowStockThreshold] = 10 
WHERE [IsVisible] IS NULL OR [LowStockThreshold] IS NULL;

-- Add computed column for stock status (optional, for display purposes)
IF NOT EXISTS (SELECT * FROM sys.computed_columns WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND name = 'StockStatus')
BEGIN
    ALTER TABLE [dbo].[Products] ADD [StockStatus] AS 
        CASE 
            WHEN [StockQuantity] <= 0 THEN 'Out of Stock'
            WHEN [StockQuantity] <= [LowStockThreshold] THEN 'Low Stock (' + CAST([StockQuantity] AS NVARCHAR) + ' left)'
            ELSE 'In Stock (' + CAST([StockQuantity] AS NVARCHAR) + ' available)'
        END;
    PRINT 'Added computed StockStatus column to Products table';
END

-- Add computed column for stock alert level
IF NOT EXISTS (SELECT * FROM sys.computed_columns WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND name = 'StockAlertLevel')
BEGIN
    ALTER TABLE [dbo].[Products] ADD [StockAlertLevel] AS 
        CASE 
            WHEN [StockQuantity] <= 0 THEN 3  -- Critical: Out of Stock
            WHEN [StockQuantity] = 1 THEN 2   -- Critical: Only 1 left
            WHEN [StockQuantity] <= [LowStockThreshold] THEN 1  -- Warning: Low Stock
            ELSE 0  -- OK
        END;
    PRINT 'Added computed StockAlertLevel column to Products table';
END

PRINT 'Stock management migration completed successfully!';