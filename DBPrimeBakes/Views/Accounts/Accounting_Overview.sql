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

    COUNT(DISTINCT ad.Id) AS TotalLedgers,

    SUM(CASE WHEN ad.Debit IS NOT NULL THEN 1 ELSE 0 END) AS TotalDebitLedgers,
    SUM(CASE WHEN ad.Credit IS NOT NULL THEN 1 ELSE 0 END) AS TotalCreditLedgers,

    SUM(ad.Debit) AS TotalDebitAmount,
    SUM(ad.Credit) AS TotalCreditAmount,

    SUM(ad.Debit) + SUM(ad.Credit) AS TotalAmount,

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
INNER JOIN
    [dbo].[AccountingDetail] ad ON a.Id = ad.AccountingId

WHERE
    [ad].[Status] = 1

GROUP BY
    [a].[Id],
    [a].[TransactionNo],
    [a].[CompanyId],
    [c].[Name],
    [a].[VoucherId],
    [v].[Name],
    [a].[ReferenceId],
    [a].[ReferenceNo],
    [a].[TransactionDateTime],
    [a].[FinancialYearId],
    [fy].StartDate,
    [fy].EndDate,
    [a].[Remarks],
    [a].[CreatedBy],
    [u].[Name],
    [a].[CreatedAt],
    [a].[CreatedFromPlatform],
    [a].[LastModifiedBy],
    [lm].[Name],
    [a].[LastModifiedAt],
    [a].[LastModifiedFromPlatform],
    [a].[Status]