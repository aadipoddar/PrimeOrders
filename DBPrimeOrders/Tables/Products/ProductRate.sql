CREATE TABLE [dbo].[ProductRate]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [ProductId] INT NOT NULL, 
    [Rate] MONEY NOT NULL, 
    [LocationId] INT NOT NULL, 
    [Status] BIT NOT NULL, 
    CONSTRAINT [FK_ProductRate_ToProduct] FOREIGN KEY (ProductId) REFERENCES [Product](Id), 
    CONSTRAINT [FK_ProductRate_ToLocation] FOREIGN KEY (LocationId) REFERENCES [Location](Id)
)
