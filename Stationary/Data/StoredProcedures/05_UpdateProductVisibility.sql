-- =============================================
-- Stored Procedure: usp_UpdateProductVisibility
-- Purpose: Automatically update product visibility based on stock levels
-- =============================================

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
        
        SELECT @@ROWCOUNT AS HiddenProducts;
    END
    ELSE
    BEGIN
        -- Show all products
        UPDATE Products 
        SET IsVisible = 1;
        
        SELECT @@ROWCOUNT AS ShownProducts;
    END
END
GO

PRINT 'Stored Procedure usp_UpdateProductVisibility created successfully!';