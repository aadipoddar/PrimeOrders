CREATE TABLE [dbo].[Ledger]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Name] VARCHAR(500) NOT NULL, 
    [GroupId] INT NOT NULL, 
    [AccountTypeId] INT NOT NULL, 
    [Code] VARCHAR(10) NOT NULL UNIQUE, 
    [StateUTId] INT NOT NULL, 
    [GSTNo] VARCHAR(MAX) NULL, 
    [PANNo] VARCHAR(MAX) NULL,
    [CINNo] VARCHAR(MAX) NULL,
    [Alias] VARCHAR(MAX) NULL, 
    [Phone] VARCHAR(20) NULL, 
    [Email] VARCHAR(MAX) NULL, 
    [Address] VARCHAR(MAX) NULL, 
    [Remarks] VARCHAR(MAX) NULL, 
    [LocationId] INT NULL, 
    [Status] BIT NOT NULL DEFAULT 1, 
    CONSTRAINT [FK_Ledger_ToGroup] FOREIGN KEY (GroupId) REFERENCES [Group](Id), 
    CONSTRAINT [FK_Ledger_ToAccountType] FOREIGN KEY (AccountTypeId) REFERENCES [AccountType](Id),
    CONSTRAINT [FK_Ledger_ToLocation] FOREIGN KEY (LocationId) REFERENCES [Location](Id), 
    CONSTRAINT [FK_Ledger_ToStateUT] FOREIGN KEY ([StateUTId]) REFERENCES [StateUT](Id)
)