CREATE TABLE [dbo].[SaleReturn]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [TransactionNo] VARCHAR(20) NOT NULL, 
    [Remarks] VARCHAR(250) NOT NULL, 
    [ReturnDateTime] DATETIME NOT NULL DEFAULT (((getdate() AT TIME ZONE 'UTC') AT TIME ZONE 'India Standard Time')),
    [UserId] INT NOT NULL, 
    [LocationId] INT NOT NULL,
    [Status] BIT NOT NULL DEFAULT 1, 
    CONSTRAINT [FK_SaleReturn_ToUser] FOREIGN KEY (UserId) REFERENCES [User](Id), 
    CONSTRAINT [FK_SaleReturn_ToLocation] FOREIGN KEY (LocationId) REFERENCES [Location](Id), 
)
