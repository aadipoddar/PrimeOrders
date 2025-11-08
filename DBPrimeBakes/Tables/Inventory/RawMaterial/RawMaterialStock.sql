CREATE TABLE [dbo].[RawMaterialStock]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [RawMaterialId] INT NOT NULL ,
    [Quantity] MONEY NOT NULL,
    [NetRate] MONEY NULL, 
    [Type] VARCHAR(20) NOT NULL, 
    [TransactionId] INT NULL, 
    [TransactionNo] VARCHAR(MAX) NOT NULL, 
    [TransactionDate] DATE NOT NULL, 
    CONSTRAINT [FK_RawMaterialStock_ToRawMaterial] FOREIGN KEY (RawMaterialId) REFERENCES [RawMaterial](Id)
)
