CREATE TABLE [dbo].[AccountingDetails]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [AccountingId] INT NOT NULL, 
    [Type] CHAR(1) NOT NULL CHECK (Type IN ('D', 'C')),
    [LedgerId] INT NOT NULL, 
    [Amount] MONEY NOT NULL, 
    [Remarks] VARCHAR(500) NOT NULL, 
    [Status] BIT NOT NULL DEFAULT 1, 
    CONSTRAINT [FK_AccountingDetails_ToAccounting] FOREIGN KEY (AccountingId) REFERENCES [Accounting](Id), 
    CONSTRAINT [FK_AccountingDetails_ToLedger] FOREIGN KEY (LedgerId) REFERENCES [Ledger](Id)

)
