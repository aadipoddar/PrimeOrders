CREATE TABLE [dbo].[Ledger]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Name] VARCHAR(250) NOT NULL, 
    [GroupId] INT NOT NULL, 
    [AccountTypeId] INT NOT NULL, 
    [Code] VARCHAR(10) NOT NULL UNIQUE, 
    [Phone] VARCHAR(20) NOT NULL, 
    [Address] VARCHAR(250) NOT NULL, 
    [GSTNo] VARCHAR(20) NOT NULL, 
    [Remarks] VARCHAR(250) NOT NULL, 
    [StateId] INT NOT NULL, 
    [LocationId] INT NULL, 
    [Status] BIT NOT NULL DEFAULT 1, 
    CONSTRAINT [FK_Ledger_ToGroup] FOREIGN KEY (GroupId) REFERENCES [Group](Id), 
    CONSTRAINT [FK_Ledger_ToAccountType] FOREIGN KEY (AccountTypeId) REFERENCES [AccountType](Id), 
    CONSTRAINT [FK_Ledger_ToLocation] FOREIGN KEY (LocationId) REFERENCES [Location](Id), 
    CONSTRAINT [FK_Ledger_ToState] FOREIGN KEY (StateId) REFERENCES [State](Id)
)