CREATE TABLE [dbo].[Purchase]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [BillNo] VARCHAR(50) NOT NULL, 
    [SupplierId] INT NOT NULL, 
    [BillDate] DATE NOT NULL, 
    [CDPercent] DECIMAL(5, 2) NOT NULL DEFAULT 0, 
    [CDAmount] MONEY NOT NULL DEFAULT 0, 
    [Remarks] VARCHAR(250) NOT NULL, 
    [UserId] INT NOT NULL, 
    [Status] BIT NOT NULL DEFAULT 1, 
    CONSTRAINT [FK_Purchase_ToSupplier] FOREIGN KEY (SupplierId) REFERENCES [Supplier](Id), 
    CONSTRAINT [FK_Purchase_ToUser] FOREIGN KEY (UserId) REFERENCES [User](Id)
)
