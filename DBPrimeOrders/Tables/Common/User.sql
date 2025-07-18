CREATE TABLE [dbo].[User]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Name] VARCHAR(50) NOT NULL, 
    [Passcode] SMALLINT NOT NULL UNIQUE, 
    [LocationId] INT NOT NULL, 
    [Sales] BIT NOT NULL DEFAULT 0, 
    [Order] BIT NOT NULL DEFAULT 0, 
    [Inventory] BIT NOT NULL DEFAULT 0, 
    [Accounts] BIT NOT NULL DEFAULT 0, 
    [Admin] BIT NOT NULL DEFAULT 0, 
    [Status] BIT NOT NULL DEFAULT 1, 
    CONSTRAINT [FK_Users_ToLocation] FOREIGN KEY (LocationId) REFERENCES [Location](Id)
)
