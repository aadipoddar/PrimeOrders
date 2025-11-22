CREATE VIEW [dbo].[Ledger_Overview]
	AS
SELECT
	[l].Id AS LedgerId,
	[l].[Name] AS LedgerName,
	[l].[Alias] AS LedgerAlias,
	[l].[Code] AS LedgerCode,
	[l].[AccountTypeId],
	[at].[Name] AS AccountTypeName,
	[l].[GroupId],
	[g].[Name] AS GroupName,
	[l].[Address],
	[l].[GSTNo],
	[l].[Phone],
	[l].[Email],
	[l].[StateUTId],
	[s].[Name] AS StateName,
	[l].[Remarks] AS LedgerRemarks,
	[l].[LocationId] AS LedgerLocationId,
	[lo].[Name] AS LedgerLocationName,
	[a].[Id] AS AccountingId,
	[a].[AccountingDate],
	[a].[TransactionNo],
	[a].[Remarks] AS AccountingRemarks,
	[ad].[ReferenceId],
	[ad].[ReferenceType],

	(CASE 
		WHEN [ad].[ReferenceType] = 'Sales' THEN
			(SELECT TransactionNo FROM [dbo].[Sale_Overview] WHERE Id = [ad].[ReferenceId])
		WHEN [ad].[ReferenceType] = 'Purchase' THEN
			(SELECT TransactionNo FROM [dbo].[Purchase_Overview] WHERE Id = [ad].[ReferenceId])
		WHEN [ad].[ReferenceType] = 'SaleReturn' THEN
			(SELECT TransactionNo FROM [dbo].[SaleReturn_Overview] WHERE Id = [ad].[ReferenceId])
	END) AS ReferenceNo,

	(CASE
		WHEN [ad].[ReferenceType] = 'Sales' THEN
			(SELECT TransactionDateTime FROM [dbo].[Sale_Overview] WHERE Id = [ad].[ReferenceId])
		WHEN [ad].[ReferenceType] = 'Purchase' THEN
			(SELECT TransactionDateTime FROM [dbo].[Purchase_Overview] WHERE Id = [ad].[ReferenceId])
		WHEN [ad].[ReferenceType] = 'SaleReturn' THEN
			(SELECT TransactionDateTime FROM [dbo].[SaleReturn_Overview] WHERE Id = [ad].[ReferenceId])
	END) AS ReferenceDate,

	(CASE
		WHEN [ad].[ReferenceType] = 'Sales' THEN
			(SELECT TotalAmount FROM [dbo].[Sale_Overview] WHERE Id = [ad].[ReferenceId])
		WHEN [ad].[ReferenceType] = 'Purchase' THEN
			(SELECT TotalAmount FROM [dbo].[Purchase_Overview] WHERE Id = [ad].[ReferenceId])
		WHEN [ad].[ReferenceType] = 'SaleReturn' THEN
			(SELECT TotalAmount FROM [dbo].[SaleReturn_Overview] WHERE Id = [ad].[ReferenceId])
	END) AS ReferenceAmount,

	SUM([ad].[Debit]) AS Debit,
	SUM([ad].[Credit]) AS Credit,

	MAX([ad].[Remarks]) AS Remarks

FROM
	[dbo].[AccountingDetails] ad

INNER JOIN
	[dbo].[Accounting] a ON ad.AccountingId = a.Id
INNER JOIN
	[dbo].[Ledger] l ON ad.LedgerId = l.Id
INNER JOIN
	[dbo].[AccountType] at ON l.AccountTypeId = at.Id
INNER JOIN
	[dbo].[Group] g ON l.GroupId = g.Id
INNER JOIN
	[dbo].[StateUT] s ON l.[StateUTId] = s.Id
LEFT JOIN
	[dbo].[Location] lo ON l.LocationId = lo.Id

WHERE
	[a].[Status] = 1 AND
	[ad].[Status] = 1

GROUP BY
	[a].Id,
	[l].Id,
	[l].[Name],
	[l].[Alias],
	[l].[Code],
	[l].[AccountTypeId],
	[at].[Name],
	[l].[GroupId],
	[g].[Name],
	[l].[Address],
	[l].[GSTNo],
	[l].[Phone],
	[l].[Email],
	[l].[StateUTId],
	[s].[Name],
	[l].[Remarks],
	[l].[LocationId],
	[lo].[Name],
	[a].[AccountingDate],
	[a].[TransactionNo],
	[ad].[ReferenceId],
	[ad].[ReferenceType],
	[a].[Remarks]