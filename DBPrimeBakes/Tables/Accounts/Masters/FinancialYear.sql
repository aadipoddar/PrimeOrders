CREATE TABLE [dbo].[FinancialYear]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY,
	[StartDate] DATE NOT NULL,
	[EndDate] DATE NOT NULL, 
    [YearNo] INT NOT NULL,
    [Remarks] VARCHAR(MAX) NULL,
	[Locked] BIT NOT NULL DEFAULT 0,
    [Status] BIT NOT NULL DEFAULT 1, 
)
