CREATE TABLE [dbo].[OrderDetail]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [OrderId] INT NOT NULL, 
    [ProductId] INT NOT NULL, 
    [Quantity] MONEY NOT NULL DEFAULT 1, 
    [Remarks] VARCHAR(MAX) NULL,
    [Status] BIT NOT NULL DEFAULT 1, 
    CONSTRAINT [FK_OrderDetail_ToOrder] FOREIGN KEY (OrderId) REFERENCES [Order](Id), 
    CONSTRAINT [FK_OrderDetail_ToProduct] FOREIGN KEY (ProductId) REFERENCES [Product](Id)
)
