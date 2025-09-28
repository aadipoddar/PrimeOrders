CREATE TABLE [dbo].[Accounting]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [ReferenceNo] VARCHAR(20) NOT NULL, 
    [VoucherId] INT NOT NULL, 
    [Remarks] VARCHAR(500) NOT NULL, 
    [AccountingDate] DATE NOT NULL, 
    [FinancialYearId] INT NOT NULL, 
    [UserId] INT NOT NULL, 
    [GeneratedModule] VARCHAR(20) NOT NULL , 
    [Status] BIT NOT NULL DEFAULT 1, 
    CONSTRAINT [FK_Accounting_ToVoucher] FOREIGN KEY (VoucherId) REFERENCES [Voucher](Id), 
    CONSTRAINT [FK_Accounting_ToFinancialYear] FOREIGN KEY (FinancialYearId) REFERENCES [FinancialYear](Id), 
    CONSTRAINT [FK_Accounting_ToUser] FOREIGN KEY (UserId) REFERENCES [User](Id)

)
