CREATE TABLE [dbo].[Purchase]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [BillNo] VARCHAR(50) NOT NULL, 
    [SupplierId] INT NOT NULL, 
    [BillDateTime] DATETIME NOT NULL, 
    [CDPercent] DECIMAL(5, 2) NOT NULL DEFAULT 0, 
    [RoundOff] MONEY NOT NULL DEFAULT 0, 
    [Remarks] VARCHAR(250) NOT NULL, 
    [UserId] INT NOT NULL, 
    [CreatedAt] DATETIME NOT NULL DEFAULT (((getdate() AT TIME ZONE 'UTC') AT TIME ZONE 'India Standard Time')), 
    [Status] BIT NOT NULL DEFAULT 1, 
    CONSTRAINT [FK_Purchase_ToLedger] FOREIGN KEY (SupplierId) REFERENCES [Ledger](Id), 
    CONSTRAINT [FK_Purchase_ToUser] FOREIGN KEY (UserId) REFERENCES [User](Id)
)
