﻿CREATE TABLE [dbo].[Order]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [OrderNo] VARCHAR(20) NOT NULL UNIQUE, 
    [OrderDate] DATE NOT NULL, 
    [LocationId] INT NOT NULL, 
    [UserId] INT NOT NULL, 
    [Remarks] VARCHAR(250) NOT NULL, 
    [SaleId] INT NULL , 
    [Status] BIT NOT NULL DEFAULT 1, 
    CONSTRAINT [FK_Order_ToLocation] FOREIGN KEY (LocationId) REFERENCES [Location](Id), 
    CONSTRAINT [FK_Order_ToUser] FOREIGN KEY (UserId) REFERENCES [User](Id), 
    CONSTRAINT [FK_Order_ToSale] FOREIGN KEY ([SaleId]) REFERENCES [Sale](Id)
)
