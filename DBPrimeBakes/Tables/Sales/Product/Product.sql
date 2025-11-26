CREATE TABLE [dbo].[Product]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Name] VARCHAR(500) NOT NULL, 
    [Code] VARCHAR(10) NOT NULL UNIQUE, 
    [ProductCategoryId] INT NOT NULL, 
    [Rate] MONEY NOT NULL, 
    [TaxId] INT NOT NULL, 
    [Remarks] VARCHAR(MAX) NULL,
    [Status] BIT NOT NULL DEFAULT 1, 
    CONSTRAINT [FK_Item_ToItemCategory] FOREIGN KEY ([ProductCategoryId]) REFERENCES [ProductCategory](Id), 
    CONSTRAINT [FK_Item_ToTax] FOREIGN KEY (TaxId) REFERENCES [Tax](Id), 
)
