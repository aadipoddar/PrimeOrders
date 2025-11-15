CREATE TABLE [dbo].[KitchenProduction]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [TransactionNo] VARCHAR(MAX) NOT NULL, 
	[CompanyId] INT NOT NULL,
	[TransactionDateTime] DATETIME NOT NULL,
	[FinancialYearId] INT NOT NULL,
    [KitchenId] INT NOT NULL,
	[TotalAmount] MONEY NOT NULL DEFAULT 0,
	[Remarks] VARCHAR(MAX) NULL,
	[CreatedBy] INT NOT NULL,
	[CreatedAt] DATETIME NOT NULL DEFAULT (((getdate() AT TIME ZONE 'UTC') AT TIME ZONE 'India Standard Time')),
	[CreatedFromPlatform] VARCHAR(MAX) NOT NULL,
	[Status] BIT NOT NULL DEFAULT 1,
	[LastModifiedBy] INT NULL,
	[LastModifiedAt] DATETIME NULL, 
	[LastModifiedFromPlatform] VARCHAR(MAX) NULL,
    CONSTRAINT [FK_KitchenProduction_ToCompany] FOREIGN KEY ([CompanyId]) REFERENCES [Company]([Id]),
    CONSTRAINT [FK_KitchenProduction_ToFinancialYear] FOREIGN KEY ([FinancialYearId]) REFERENCES [dbo].[FinancialYear]([Id]),
    CONSTRAINT [FK_KitchenProduction_ToKitchen] FOREIGN KEY ([KitchenId]) REFERENCES [Kitchen]([Id]),
    CONSTRAINT [FK_KitchenProduction_ToUser] FOREIGN KEY ([CreatedBy]) REFERENCES [User]([Id]),
	CONSTRAINT [FK_KitchenProduction_LastModifiedBy_ToUser] FOREIGN KEY ([LastModifiedBy]) REFERENCES [User]([Id])
)
