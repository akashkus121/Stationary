-- Stored Procedures for Advanced Stock Management
-- Run these scripts to add performance-optimized stock operations

-- 1. Get Stock Alert Summary (replaces multiple EF queries)
CREATE PROCEDURE [dbo].[usp_GetStockAlertSummary]
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        COUNT(*) AS TotalProducts,
        SUM(CASE WHEN StockQuantity > LowStockThreshold THEN 1 ELSE 0 END) AS InStockProducts,
        SUM(CASE WHEN StockQuantity > 0 AND StockQuantity <= LowStockThreshold THEN 1 ELSE 0 END) AS LowStockProducts,
        SUM(CASE WHEN StockQuantity <= 0 THEN 1 ELSE 0 END) AS OutOfStockProducts,
        SUM(CASE WHEN StockQuantity = 1 THEN 1 ELSE 0 END) AS CriticalStockProducts
    FROM Products
    WHERE IsVisible = 1;
END
GO

-- 2. Get Products by Stock Status (optimized filtering)
CREATE PROCEDURE [dbo].[usp_GetProductsByStockStatus]
    @StockStatus NVARCHAR(20), -- 'available', 'lowstock', 'outofstock', 'all'
    @Category NVARCHAR(50) = NULL,
    @SearchTerm NVARCHAR(100) = NULL,
    @Page INT = 1,
    @PageSize INT = 20
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Offset INT = (@Page - 1) * @PageSize;
    
    -- Build dynamic SQL for flexible filtering
    DECLARE @SQL NVARCHAR(MAX) = '
    SELECT 
        Id, Name, Category, Price, ImagePath, StockQuantity, LowStockThreshold, IsVisible,
        CASE 
            WHEN StockQuantity <= 0 THEN ''Out of Stock''
            WHEN StockQuantity <= LowStockThreshold THEN ''Low Stock ('' + CAST(StockQuantity AS NVARCHAR) + '' left)''
            ELSE ''In Stock ('' + CAST(StockQuantity AS NVARCHAR) + '' available)''
        END AS StockStatus
    FROM Products
    WHERE IsVisible = 1';
    
    -- Add stock status filter
    IF @StockStatus = 'available'
        SET @SQL = @SQL + ' AND StockQuantity > LowStockThreshold';
    ELSE IF @StockStatus = 'lowstock'
        SET @SQL = @SQL + ' AND StockQuantity > 0 AND StockQuantity <= LowStockThreshold';
    ELSE IF @StockStatus = 'outofstock'
        SET @SQL = @SQL + ' AND StockQuantity <= 0';
    -- 'all' shows everything
    
    -- Add category filter
    IF @Category IS NOT NULL
        SET @SQL = @SQL + ' AND Category = @Category';
    
    -- Add search filter
    IF @SearchTerm IS NOT NULL
        SET @SQL = @SQL + ' AND (Name LIKE ''%'' + @SearchTerm + ''%'' OR Category LIKE ''%'' + @SearchTerm + ''%'')';
    
    -- Add pagination
    SET @SQL = @SQL + ' ORDER BY Name OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY';
    
    EXEC sp_executesql @SQL, 
        N'@Category NVARCHAR(50), @SearchTerm NVARCHAR(100), @Offset INT, @PageSize INT',
        @Category, @SearchTerm, @Offset, @PageSize;
END
GO

-- 3. Bulk Update Stock (more efficient than individual updates)
CREATE PROCEDURE [dbo].[usp_BulkUpdateStock]
    @Updates dbo.StockUpdateTableType READONLY
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    
    BEGIN TRY
        UPDATE p
        SET 
            p.StockQuantity = u.NewStockQuantity,
            p.LowStockThreshold = u.NewLowStockThreshold
        FROM Products p
        INNER JOIN @Updates u ON p.Id = u.ProductId;
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- 4. Get Low Stock Alerts (for notifications)
CREATE PROCEDURE [dbo].[usp_GetLowStockAlerts]
    @Threshold INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        Id, Name, Category, Price, StockQuantity, LowStockThreshold,
        CASE 
            WHEN StockQuantity = 0 THEN 'CRITICAL: Out of Stock'
            WHEN StockQuantity = 1 THEN 'CRITICAL: Only 1 left'
            WHEN StockQuantity <= LowStockThreshold THEN 'WARNING: Low Stock'
            ELSE 'OK'
        END AS AlertLevel
    FROM Products
    WHERE IsVisible = 1 
        AND (StockQuantity = 0 OR StockQuantity <= ISNULL(@Threshold, LowStockThreshold))
    ORDER BY 
        CASE WHEN StockQuantity = 0 THEN 1 WHEN StockQuantity = 1 THEN 2 ELSE 3 END,
        StockQuantity ASC;
END
GO

-- 5. Update Product Visibility Based on Stock
CREATE PROCEDURE [dbo].[usp_UpdateProductVisibility]
    @AutoHideOutOfStock BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    
    IF @AutoHideOutOfStock = 1
    BEGIN
        -- Hide out-of-stock products
        UPDATE Products 
        SET IsVisible = 0 
        WHERE StockQuantity <= 0;
    END
    ELSE
    BEGIN
        -- Show all products
        UPDATE Products 
        SET IsVisible = 1;
    END
END
GO