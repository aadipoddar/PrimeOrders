CREATE TABLE [dbo].[RawMaterial]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Name] VARCHAR(250) NOT NULL, 
    [Code] VARCHAR(50) NOT NULL, 
    [RawMaterialCategoryId] INT NOT NULL, 
    [MRP] MONEY NOT NULL, 
    [TaxId] INT NOT NULL, 
    [Status] BIT NOT NULL DEFAULT 1, 
    CONSTRAINT [FK_RawMaterial_ToRawMaterialCategory] FOREIGN KEY ([RawMaterialCategoryId]) REFERENCES [RawMaterialCategory](Id),
    CONSTRAINT [FK_RawMaterial_ToTax] FOREIGN KEY (TaxId) REFERENCES [Tax](Id) 
)
