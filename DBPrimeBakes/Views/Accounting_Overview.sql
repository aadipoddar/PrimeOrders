CREATE VIEW [dbo].[Accounting_Overview]
AS
SELECT
    [a].[Id] AS AccountingId,
    [a].[ReferenceNo],
    [a].[UserId],
    [u].[Name] AS UserName,
    [a].[AccountingDate],
    [a].[VoucherId],
    [v].[Name] AS VoucherName,
    [a].[FinancialYearId],
    FORMAT(fy.StartDate, 'dd/MM/yyyy') + ' - ' + FORMAT(fy.EndDate, 'dd/MM/yyyy') AS FinancialYearPeriod,
    [a].[GeneratedModule],
    [a].[Remarks],

    COUNT(DISTINCT ad.Id) AS TotalLedgers,

    SUM(CASE WHEN ad.Debit IS NOT NULL THEN 1 ELSE 0 END) AS TotalDebitLedgers,
    SUM(CASE WHEN ad.Credit IS NOT NULL THEN 1 ELSE 0 END) AS TotalCreditLedgers,

    SUM(ad.Debit) AS TotalDebitAmount,
    SUM(ad.Credit) AS TotalCreditAmount,

    SUM(ad.Debit) + SUM(ad.Credit) AS TotalAmount,

    [a].[CreatedAt]

FROM
    [dbo].[Accounting] a
INNER JOIN
    [dbo].[AccountingDetails] ad ON a.Id = ad.AccountingId
INNER JOIN
    [dbo].[User] u ON a.UserId = u.Id
INNER JOIN
    [dbo].[Voucher] v ON a.VoucherId = v.Id
INNER JOIN
    [dbo].[FinancialYear] fy ON a.FinancialYearId = fy.Id
WHERE
    [a].[Status] = 1
    AND [ad].[Status] = 1
GROUP BY
    [a].[Id],
    [a].[ReferenceNo],
    [a].[UserId],
    [u].[Name],
    [a].[AccountingDate],
    [v].[Name],
    [a].[VoucherId],
    [a].[FinancialYearId],
    [a].[GeneratedModule],
    [a].[Remarks],
    [fy].[StartDate],
    [fy].[EndDate],
    [a].[CreatedAt]