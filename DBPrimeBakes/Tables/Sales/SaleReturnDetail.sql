﻿CREATE TABLE [dbo].[SaleReturnDetail]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [SaleReturnId] INT NOT NULL, 
    [ProductId] INT NOT NULL, 
    [Quantity] DECIMAL(5, 2) NOT NULL, 
    [Rate] MONEY NOT NULL,
    [BaseTotal] MONEY NOT NULL DEFAULT 0, 
    [DiscPercent] DECIMAL(5, 2) NOT NULL DEFAULT 0, 
    [DiscAmount] MONEY NOT NULL DEFAULT 0,
    [AfterDiscount] MONEY NOT NULL DEFAULT 0, 
    [CGSTPercent] DECIMAL(5, 2) NOT NULL DEFAULT 0, 
    [CGSTAmount] MONEY NOT NULL DEFAULT 0,
    [SGSTPercent] DECIMAL(5, 2) NOT NULL DEFAULT 0, 
    [SGSTAmount] MONEY NOT NULL DEFAULT 0, 
    [IGSTPercent] DECIMAL(5, 2) NOT NULL DEFAULT 0, 
    [IGSTAmount] MONEY NOT NULL DEFAULT 0, 
    [Total] MONEY NOT NULL DEFAULT 0, 
    [NetRate] MONEY NOT NULL DEFAULT 0, 
    [Status] BIT NOT NULL DEFAULT 1, 
    CONSTRAINT [FK_SaleReturnDetail_ToSaleReturn] FOREIGN KEY (SaleReturnId) REFERENCES [SaleReturn](Id), 
    CONSTRAINT [FK_SaleReturnDetail_ToProduct] FOREIGN KEY (ProductId) REFERENCES [Product](Id)
)
