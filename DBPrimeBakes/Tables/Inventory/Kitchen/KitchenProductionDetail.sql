CREATE TABLE [dbo].[KitchenProductionDetail]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [MasterId] INT NOT NULL, 
    [ProductId] INT NOT NULL,
	[Quantity] MONEY NOT NULL DEFAULT 1,
	[Rate] MONEY NOT NULL,
    [Total] MONEY NOT NULL DEFAULT 0,
    [Remarks] VARCHAR(MAX) NULL,
	[Status] BIT NOT NULL DEFAULT 1, 
    CONSTRAINT [FK_KitchenProductionDetail_ToKitchenProduction] FOREIGN KEY ([MasterId]) REFERENCES [KitchenProduction](Id), 
    CONSTRAINT [FK_KitchenProductionDetail_ToProduct] FOREIGN KEY (ProductId) REFERENCES [Product](Id)
)
