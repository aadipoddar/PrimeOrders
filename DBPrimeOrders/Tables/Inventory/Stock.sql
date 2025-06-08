CREATE TABLE [dbo].[Stock]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [RawMaterialId] INT NOT NULL ,
    [Quantity] DECIMAL(7, 3) NOT NULL,
    [Type] VARCHAR(20) NOT NULL, 
    [BillId] INT NOT NULL, 
    [TransactionDate] DATE NOT NULL, 
    [LocationId] INT NOT NULL, 
    CONSTRAINT [FK_Stock_ToRawMaterial] FOREIGN KEY (RawMaterialId) REFERENCES [RawMaterial](Id), 
    CONSTRAINT [FK_Stock_ToLocation] FOREIGN KEY (LocationId) REFERENCES [Location](Id)
)
