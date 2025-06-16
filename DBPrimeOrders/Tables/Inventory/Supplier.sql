CREATE TABLE [dbo].[Supplier]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Name] VARCHAR(250) NOT NULL, 
    [Code] VARCHAR(50) NOT NULL, 
    [GSTNo] VARCHAR(20) NOT NULL, 
    [Phone] VARCHAR(20) NOT NULL, 
    [Email] VARCHAR(100) NOT NULL, 
    [Address] VARCHAR(500) NOT NULL, 
    [StateId] INT NOT NULL, 
    [LocationId] INT NULL, 
    [Status] BIT NOT NULL DEFAULT 1, 
    CONSTRAINT [FK_Supplier_ToState] FOREIGN KEY (StateId) REFERENCES [State](Id)
)
