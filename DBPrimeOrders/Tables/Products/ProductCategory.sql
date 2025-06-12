CREATE TABLE [dbo].[ProductCategory]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Name] VARCHAR(50) NOT NULL, 
    [LocationId] INT NOT NULL DEFAULT 1, 
    [Status] BIT NOT NULL DEFAULT 1, 
    CONSTRAINT [FK_ProductCategory_ToLocation] FOREIGN KEY (LocationId) REFERENCES [Location](Id)
)
