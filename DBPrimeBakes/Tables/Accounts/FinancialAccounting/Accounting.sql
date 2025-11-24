CREATE TABLE [dbo].[Accounting]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [TransactionNo] VARCHAR(MAX) NOT NULL, 
    [CompanyId] INT NOT NULL,
    [VoucherId] INT NOT NULL, 
	[ReferenceId] INT NULL,
	[ReferenceNo] VARCHAR(MAX) NULL,
    [TransactionDateTime] DATETIME NOT NULL,
	[FinancialYearId] INT NOT NULL,
    [Remarks] VARCHAR(MAX) NULL,
	[CreatedBy] INT NOT NULL,
	[CreatedAt] DATETIME NOT NULL DEFAULT (((getdate() AT TIME ZONE 'UTC') AT TIME ZONE 'India Standard Time')),
	[CreatedFromPlatform] VARCHAR(MAX) NOT NULL,
	[Status] BIT NOT NULL DEFAULT 1,
	[LastModifiedBy] INT NULL,
	[LastModifiedAt] DATETIME NULL, 
	[LastModifiedFromPlatform] VARCHAR(MAX) NULL, 
	CONSTRAINT [FK_Accounting_ToCompany] FOREIGN KEY ([CompanyId]) REFERENCES [Company]([Id]),
    CONSTRAINT [FK_Accounting_ToVoucher] FOREIGN KEY (VoucherId) REFERENCES [Voucher](Id), 
    CONSTRAINT [FK_Accounting_ToFinancialYear] FOREIGN KEY (FinancialYearId) REFERENCES [FinancialYear](Id), 
    CONSTRAINT [FK_Accounting_ToUser] FOREIGN KEY ([CreatedBy]) REFERENCES [User]([Id]),
	CONSTRAINT [FK_Accounting_LastModifiedBy_ToUser] FOREIGN KEY ([LastModifiedBy]) REFERENCES [User]([Id])

)
