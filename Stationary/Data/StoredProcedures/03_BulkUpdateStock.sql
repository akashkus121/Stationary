-- =============================================
-- Stored Procedure: usp_BulkUpdateStock
-- Purpose: Efficiently update stock levels for multiple products
-- =============================================

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
        
        SELECT @@ROWCOUNT AS UpdatedRows;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

PRINT 'Stored Procedure usp_BulkUpdateStock created successfully!';