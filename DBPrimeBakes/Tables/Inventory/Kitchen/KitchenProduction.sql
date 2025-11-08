CREATE TABLE [dbo].[KitchenProduction]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [KitchenId] INT NOT NULL, 
    [LocationId] INT NOT NULL, 
    [UserId] INT NOT NULL, 
    [TransactionNo] VARCHAR(20) NOT NULL UNIQUE, 
    [ProductionDate] DATETIME NOT NULL, 
    [Remarks] VARCHAR(250) NOT NULL, 
    [CreatedAt] DATETIME NOT NULL DEFAULT (((getdate() AT TIME ZONE 'UTC') AT TIME ZONE 'India Standard Time')),
    [Status] BIT NOT NULL DEFAULT 1, 
    CONSTRAINT [FK_KitchenProduction_ToKitchen] FOREIGN KEY (KitchenId) REFERENCES [Kitchen](Id), 
    CONSTRAINT [FK_KitchenProduction_ToLocation] FOREIGN KEY (LocationId) REFERENCES [Location](Id), 
    CONSTRAINT [FK_KitchenProduction_ToUser] FOREIGN KEY (UserId) REFERENCES [User](Id)
)
