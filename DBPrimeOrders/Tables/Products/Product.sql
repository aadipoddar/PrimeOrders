CREATE TABLE [dbo].[Product]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Code] VARCHAR(50) NOT NULL UNIQUE, 
    [Name] VARCHAR(250) NOT NULL, 
    [ProductCategoryId] INT NOT NULL, 
    [Rate] MONEY NOT NULL, 
    [TaxId] INT NOT NULL, 
    [Status] BIT NOT NULL DEFAULT 1, 
    CONSTRAINT [FK_Item_ToItemCategory] FOREIGN KEY ([ProductCategoryId]) REFERENCES [ProductCategory](Id), 
    CONSTRAINT [FK_Item_ToTax] FOREIGN KEY (TaxId) REFERENCES [Tax](Id)
)
