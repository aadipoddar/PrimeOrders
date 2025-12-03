CREATE VIEW [dbo].[Accounting_Overview]
AS
SELECT
    [a].[Id],
    [a].[TransactionNo],
    [a].[CompanyId],
    [c].[Name] AS CompanyName,
    [a].[VoucherId],
    [v].[Name] AS VoucherName,

    [a].[ReferenceId],
    [a].[ReferenceNo],

    [a].[TransactionDateTime],
    [a].[FinancialYearId],
	CONVERT(VARCHAR(10), fy.StartDate, 103) + ' to ' + CONVERT(VARCHAR(10), fy.EndDate, 103) AS FinancialYear,

    [a].[TotalCreditLedgers] + [a].[TotalDebitLedgers] AS TotalLedgers,

    [a].[TotalCreditLedgers],
    [a].[TotalDebitLedgers],
    [a].[TotalDebitAmount],
    [a].[TotalCreditAmount],

    [a].[TotalDebitAmount] + [a].[TotalCreditAmount] AS TotalAmount,

    [a].[Remarks],
	[a].[CreatedBy],
	[u].[Name] AS CreatedByName,
	[a].[CreatedAt],
	[a].[CreatedFromPlatform],
	[a].[LastModifiedBy],
	[lm].[Name] AS LastModifiedByUserName,
	[a].[LastModifiedAt],
	[a].[LastModifiedFromPlatform],

	[a].[Status]

FROM
    [dbo].[Accounting] a
INNER JOIN
    [dbo].[Company] c ON a.CompanyId = c.Id
INNER JOIN
    [dbo].[Voucher] v ON a.VoucherId = v.Id
INNER JOIN
    [dbo].[FinancialYear] fy ON a.FinancialYearId = fy.Id
INNER JOIN
	[dbo].[User] AS u ON a.CreatedBy = u.Id
LEFT JOIN
	[dbo].[User] AS lm ON a.LastModifiedBy = lm.Id