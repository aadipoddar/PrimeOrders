CREATE TABLE [dbo].[KitchenIssueDetail]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [KitchenIssueId] INT NOT NULL, 
    [RawMaterialId] INT NOT NULL, 
    [Quantity] DECIMAL(7, 3) NOT NULL DEFAULT 1, 
    [Status] BIT NOT NULL DEFAULT 1, 
    CONSTRAINT [FK_KitchenIssueDetail_ToKitchenIssue] FOREIGN KEY (KitchenIssueId) REFERENCES [KitchenIssue](Id), 
    CONSTRAINT [FK_KitchenIssueDetail_ToRawMaterial] FOREIGN KEY (RawMaterialId) REFERENCES [RawMaterial](Id)
)
