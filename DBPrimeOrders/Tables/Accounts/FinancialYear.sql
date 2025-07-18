CREATE TABLE [dbo].[FinancialYear]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY,
	[StartDate] DATE NOT NULL,
	[EndDate] DATE NOT NULL, 
    [YearNo] INT NOT NULL,
    [Remarks] VARCHAR(250) NOT NULL,
    [Status] BIT NOT NULL DEFAULT 1, 
)
