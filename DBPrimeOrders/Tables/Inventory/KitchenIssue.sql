CREATE TABLE [dbo].[KitchenIssue]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [KitchenId] INT NOT NULL, 
    [LocationId] INT NOT NULL, 
    [UserId] INT NOT NULL, 
    [TransactionNo] VARCHAR(20) NOT NULL UNIQUE, 
    [IssueDate] DATETIME NOT NULL, 
    [Remarks] VARCHAR(250) NOT NULL, 
    [Status] BIT NOT NULL DEFAULT 1, 
    CONSTRAINT [FK_KitchenIssue_ToKitchen] FOREIGN KEY (KitchenId) REFERENCES [Kitchen](Id), 
    CONSTRAINT [FK_KitchenIssue_ToLocation] FOREIGN KEY (LocationId) REFERENCES [Location](Id), 
    CONSTRAINT [FK_KitchenIssue_ToUser] FOREIGN KEY (UserId) REFERENCES [User](Id)
)
