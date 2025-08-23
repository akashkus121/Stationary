-- Add performance indexes for stock management features
-- Run this script after updating your database

-- Index for stock quantity filtering (most important for performance)
CREATE NONCLUSTERED INDEX [IX_Products_StockQuantity] ON [dbo].[Products]
(
    [StockQuantity] ASC
)
INCLUDE ([Name], [Category], [Price], [ImagePath], [LowStockThreshold], [IsVisible]);

-- Index for low stock threshold queries
CREATE NONCLUSTERED INDEX [IX_Products_LowStockThreshold] ON [dbo].[Products]
(
    [LowStockThreshold] ASC,
    [StockQuantity] ASC
)
INCLUDE ([Name], [Category], [Price], [ImagePath], [IsVisible]);

-- Index for product visibility and stock status
CREATE NONCLUSTERED INDEX [IX_Products_Visibility_Stock] ON [dbo].[Products]
(
    [IsVisible] ASC,
    [StockQuantity] ASC
)
INCLUDE ([Name], [Category], [Price], [ImagePath], [LowStockThreshold]);

-- Index for category filtering with stock
CREATE NONCLUSTERED INDEX [IX_Products_Category_Stock] ON [dbo].[Products]
(
    [Category] ASC,
    [StockQuantity] ASC
)
INCLUDE ([Name], [Price], [ImagePath], [LowStockThreshold], [IsVisible]);

-- Index for search with stock status
CREATE NONCLUSTERED INDEX [IX_Products_Name_Stock] ON [dbo].[Products]
(
    [Name] ASC
)
INCLUDE ([Category], [Price], [ImagePath], [StockQuantity], [LowStockThreshold], [IsVisible]);