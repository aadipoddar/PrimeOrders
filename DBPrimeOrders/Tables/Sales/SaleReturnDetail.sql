CREATE TABLE [dbo].[SaleReturnDetail]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [SaleReturnId] INT NOT NULL, 
    [ProductId] INT NOT NULL, 
    [Quantity] DECIMAL(5, 2) NOT NULL, 
    [Status] BIT NOT NULL DEFAULT 1, 
    CONSTRAINT [FK_SaleReturnDetail_ToSaleReturn] FOREIGN KEY (SaleReturnId) REFERENCES [SaleReturn](Id), 
    CONSTRAINT [FK_SaleReturnDetail_ToProduct] FOREIGN KEY (ProductId) REFERENCES [Product](Id)
)
