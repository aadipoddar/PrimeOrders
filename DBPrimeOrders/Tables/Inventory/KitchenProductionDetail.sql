CREATE TABLE [dbo].[KitchenProductionDetail]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [KitchenProductionId] INT NOT NULL, 
    [ProductId] INT NOT NULL, 
    [Quantity] DECIMAL(7, 3) NOT NULL DEFAULT 1, 
    [Status] BIT NOT NULL DEFAULT 1, 
    CONSTRAINT [FK_KitchenProductionDetail_ToKitchenProduction] FOREIGN KEY (KitchenProductionId) REFERENCES [KitchenProduction](Id), 
    CONSTRAINT [FK_KitchenProductionDetail_ToProduct] FOREIGN KEY (ProductId) REFERENCES [Product](Id)
)
