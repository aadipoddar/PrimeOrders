﻿CREATE TABLE [dbo].[Location]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Name] VARCHAR(100) NOT NULL, 
    [Discount] DECIMAL(5, 2) NOT NULL DEFAULT 0,
    [MainLocation] BIT NOT NULL DEFAULT 0,
    [Status] BIT NOT NULL DEFAULT 1
)
