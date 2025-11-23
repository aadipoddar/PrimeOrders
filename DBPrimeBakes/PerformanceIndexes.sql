-- =============================================
-- Performance Indexes for PrimeBakes Database
-- Created to optimize view queries and report generation
-- =============================================

-- =============================================
-- SALE TABLE INDEXES
-- =============================================

-- Index for Sale foreign key lookups (used in views)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sale_CompanyId' AND object_id = OBJECT_ID('dbo.Sale'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Sale_CompanyId 
    ON dbo.Sale(CompanyId) 
    INCLUDE (Status, TransactionDateTime, LocationId, FinancialYearId)
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sale_LocationId' AND object_id = OBJECT_ID('dbo.Sale'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Sale_LocationId 
    ON dbo.Sale(LocationId) 
    INCLUDE (Status, TransactionDateTime, CompanyId, FinancialYearId)
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sale_FinancialYearId' AND object_id = OBJECT_ID('dbo.Sale'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Sale_FinancialYearId 
    ON dbo.Sale(FinancialYearId) 
    INCLUDE (Status, TransactionDateTime)
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sale_OrderId' AND object_id = OBJECT_ID('dbo.Sale'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Sale_OrderId 
    ON dbo.Sale(OrderId) 
    INCLUDE (Status, Id, TransactionNo, TransactionDateTime)
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sale_PartyId' AND object_id = OBJECT_ID('dbo.Sale'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Sale_PartyId 
    ON dbo.Sale(PartyId) 
    INCLUDE (Status)
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sale_CustomerId' AND object_id = OBJECT_ID('dbo.Sale'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Sale_CustomerId 
    ON dbo.Sale(CustomerId) 
    INCLUDE (Status)
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sale_CreatedBy' AND object_id = OBJECT_ID('dbo.Sale'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Sale_CreatedBy 
    ON dbo.Sale(CreatedBy)
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sale_LastModifiedBy' AND object_id = OBJECT_ID('dbo.Sale'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Sale_LastModifiedBy 
    ON dbo.Sale(LastModifiedBy)
END
GO

-- Composite index for common filtering (Status + Date range queries)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sale_Status_TransactionDateTime' AND object_id = OBJECT_ID('dbo.Sale'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Sale_Status_TransactionDateTime 
    ON dbo.Sale(Status, TransactionDateTime DESC) 
    INCLUDE (Id, CompanyId, LocationId, FinancialYearId)
END
GO

-- =============================================
-- SALEDETAIL TABLE INDEXES
-- =============================================

-- Critical index for Sale_Overview aggregations
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SaleDetail_SaleId_Status' AND object_id = OBJECT_ID('dbo.SaleDetail'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_SaleDetail_SaleId_Status 
    ON dbo.SaleDetail(SaleId, Status) 
    INCLUDE (Quantity, BaseTotal, DiscountPercent, DiscountAmount, AfterDiscount, 
             CGSTPercent, CGSTAmount, SGSTPercent, SGSTAmount, IGSTPercent, IGSTAmount, 
             TotalTaxAmount, Total)
END
GO

-- Index for Sale_Item_Overview queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SaleDetail_ProductId_Status' AND object_id = OBJECT_ID('dbo.SaleDetail'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_SaleDetail_ProductId_Status 
    ON dbo.SaleDetail(ProductId, Status) 
    INCLUDE (SaleId, Quantity, Rate, BaseTotal, DiscountPercent, DiscountAmount, 
             AfterDiscount, Total, NetRate)
END
GO

-- =============================================
-- ORDER TABLE INDEXES
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Order_CompanyId' AND object_id = OBJECT_ID('dbo.[Order]'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Order_CompanyId 
    ON dbo.[Order](CompanyId) 
    INCLUDE (Status, TransactionDateTime, LocationId, FinancialYearId)
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Order_LocationId' AND object_id = OBJECT_ID('dbo.[Order]'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Order_LocationId 
    ON dbo.[Order](LocationId) 
    INCLUDE (Status, TransactionDateTime, CompanyId, FinancialYearId)
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Order_FinancialYearId' AND object_id = OBJECT_ID('dbo.[Order]'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Order_FinancialYearId 
    ON dbo.[Order](FinancialYearId) 
    INCLUDE (Status, TransactionDateTime)
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Order_SaleId' AND object_id = OBJECT_ID('dbo.[Order]'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Order_SaleId 
    ON dbo.[Order](SaleId) 
    INCLUDE (Status, Id, TransactionNo, TransactionDateTime)
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Order_CreatedBy' AND object_id = OBJECT_ID('dbo.[Order]'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Order_CreatedBy 
    ON dbo.[Order](CreatedBy)
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Order_LastModifiedBy' AND object_id = OBJECT_ID('dbo.[Order]'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Order_LastModifiedBy 
    ON dbo.[Order](LastModifiedBy)
END
GO

-- Composite index for filtering
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Order_Status_TransactionDateTime' AND object_id = OBJECT_ID('dbo.[Order]'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Order_Status_TransactionDateTime 
    ON dbo.[Order](Status, TransactionDateTime DESC) 
    INCLUDE (Id, CompanyId, LocationId, FinancialYearId)
END
GO

-- =============================================
-- ORDERDETAIL TABLE INDEXES
-- =============================================

-- Critical index for Order_Overview aggregations
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OrderDetail_OrderId_Status' AND object_id = OBJECT_ID('dbo.OrderDetail'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_OrderDetail_OrderId_Status 
    ON dbo.OrderDetail(OrderId, Status) 
    INCLUDE (Id, Quantity, ProductId)
END
GO

-- Index for Order_Item_Overview queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OrderDetail_ProductId_Status' AND object_id = OBJECT_ID('dbo.OrderDetail'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_OrderDetail_ProductId_Status 
    ON dbo.OrderDetail(ProductId, Status) 
    INCLUDE (OrderId, Quantity, Remarks)
END
GO

-- =============================================
-- PURCHASE TABLE INDEXES
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Purchase_CompanyId' AND object_id = OBJECT_ID('dbo.Purchase'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Purchase_CompanyId 
    ON dbo.Purchase(CompanyId) 
    INCLUDE (Status, TransactionDateTime, FinancialYearId)
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Purchase_PartyId' AND object_id = OBJECT_ID('dbo.Purchase'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Purchase_PartyId 
    ON dbo.Purchase(PartyId) 
    INCLUDE (Status, TransactionDateTime)
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Purchase_FinancialYearId' AND object_id = OBJECT_ID('dbo.Purchase'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Purchase_FinancialYearId 
    ON dbo.Purchase(FinancialYearId) 
    INCLUDE (Status, TransactionDateTime)
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Purchase_CreatedBy' AND object_id = OBJECT_ID('dbo.Purchase'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Purchase_CreatedBy 
    ON dbo.Purchase(CreatedBy)
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Purchase_LastModifiedBy' AND object_id = OBJECT_ID('dbo.Purchase'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Purchase_LastModifiedBy 
    ON dbo.Purchase(LastModifiedBy)
END
GO

-- Composite index for filtering
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Purchase_Status_TransactionDateTime' AND object_id = OBJECT_ID('dbo.Purchase'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Purchase_Status_TransactionDateTime 
    ON dbo.Purchase(Status, TransactionDateTime DESC) 
    INCLUDE (Id, CompanyId, FinancialYearId)
END
GO

-- =============================================
-- PURCHASEDETAIL TABLE INDEXES
-- =============================================

-- Critical index for Purchase_Overview aggregations
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PurchaseDetail_PurchaseId_Status' AND object_id = OBJECT_ID('dbo.PurchaseDetail'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_PurchaseDetail_PurchaseId_Status 
    ON dbo.PurchaseDetail(PurchaseId, Status) 
    INCLUDE (Quantity, BaseTotal, DiscountPercent, DiscountAmount, AfterDiscount, 
             CGSTPercent, CGSTAmount, SGSTPercent, SGSTAmount, IGSTPercent, IGSTAmount, 
             TotalTaxAmount, Total)
END
GO

-- Index for Purchase_Item_Overview queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PurchaseDetail_RawMaterialId_Status' AND object_id = OBJECT_ID('dbo.PurchaseDetail'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_PurchaseDetail_RawMaterialId_Status 
    ON dbo.PurchaseDetail(RawMaterialId, Status) 
    INCLUDE (PurchaseId, Quantity, Rate, BaseTotal, DiscountPercent, DiscountAmount, 
             AfterDiscount, Total, NetRate)
END
GO

-- =============================================
-- PRODUCT AND RELATED TABLE INDEXES
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Product_ProductCategoryId' AND object_id = OBJECT_ID('dbo.Product'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Product_ProductCategoryId 
    ON dbo.Product(ProductCategoryId) 
    INCLUDE (Name, Code)
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RawMaterial_RawMaterialCategoryId' AND object_id = OBJECT_ID('dbo.RawMaterial'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_RawMaterial_RawMaterialCategoryId 
    ON dbo.RawMaterial(RawMaterialCategoryId) 
    INCLUDE (Name, Code)
END
GO

-- =============================================
-- STATISTICS UPDATE
-- =============================================
-- Update statistics on critical tables for query optimization
UPDATE STATISTICS dbo.Sale WITH FULLSCAN;
UPDATE STATISTICS dbo.SaleDetail WITH FULLSCAN;
UPDATE STATISTICS dbo.[Order] WITH FULLSCAN;
UPDATE STATISTICS dbo.OrderDetail WITH FULLSCAN;
UPDATE STATISTICS dbo.Purchase WITH FULLSCAN;
UPDATE STATISTICS dbo.PurchaseDetail WITH FULLSCAN;
GO
