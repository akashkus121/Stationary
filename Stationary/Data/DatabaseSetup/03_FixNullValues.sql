-- Fix Null Values in Database
-- This script fixes any existing null values that might cause SqlNullValueException

USE StationaryDB;
GO

-- Fix null Category values
UPDATE Products 
SET Category = 'Uncategorized' 
WHERE Category IS NULL OR Category = '';

-- Fix null Name values (if any)
UPDATE Products 
SET Name = 'Unnamed Product' 
WHERE Name IS NULL OR Name = '';

-- Fix null Price values
UPDATE Products 
SET Price = 0.01 
WHERE Price IS NULL;

-- Fix null StockQuantity values
UPDATE Products 
SET StockQuantity = 0 
WHERE StockQuantity IS NULL;

-- Fix null LowStockThreshold values
UPDATE Products 
SET LowStockThreshold = 5 
WHERE LowStockThreshold IS NULL;

-- Set default visibility
UPDATE Products 
SET IsVisible = 1 
WHERE IsVisible IS NULL;

-- Verify no null values exist
SELECT 
    'Products with null Category' as CheckType,
    COUNT(*) as Count
FROM Products 
WHERE Category IS NULL OR Category = ''

UNION ALL

SELECT 
    'Products with null Name' as CheckType,
    COUNT(*) as Count
FROM Products 
WHERE Name IS NULL OR Name = ''

UNION ALL

SELECT 
    'Products with null Price' as CheckType,
    COUNT(*) as Count
FROM Products 
WHERE Price IS NULL

UNION ALL

SELECT 
    'Products with null StockQuantity' as CheckType,
    COUNT(*) as Count
FROM Products 
WHERE StockQuantity IS NULL

UNION ALL

SELECT 
    'Products with null LowStockThreshold' as CheckType,
    COUNT(*) as Count
FROM Products 
WHERE LowStockThreshold IS NULL;

GO

PRINT 'Database null value cleanup completed successfully!';