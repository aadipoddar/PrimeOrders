CREATE VIEW [dbo].[Accounting_Ledger_Overview]
	AS
SELECT
	[l].[Id],
	[l].[Name] AS LedgerName,
	[l].[Code] AS LedgerCode,
	[l].[AccountTypeId],
	[at].[Name] AS AccountTypeName,
	[l].[GroupId],
	[g].[Name] AS GroupName,

	[a].[Id] AS AccountingId,
	[a].[TransactionNo],
	[a].[TransactionDateTime],
	[a].[CompanyId],
	[c].[Name] AS CompanyName,
	[a].[Remarks] AS AccountingRemarks,

	[ad].[ReferenceId],
	[ad].[ReferenceType],
	[ad].[ReferenceNo],

	(CASE
		WHEN [ad].[ReferenceType] = 'Sale' THEN
			(SELECT TransactionDateTime FROM [dbo].[Sale_Overview] WHERE Id = [ad].[ReferenceId])
		WHEN [ad].[ReferenceType] = 'SaleReturn' THEN
			(SELECT TransactionDateTime FROM [dbo].[SaleReturn_Overview] WHERE Id = [ad].[ReferenceId])
		WHEN [ad].[ReferenceType] = 'Purchase' THEN
			(SELECT TransactionDateTime FROM [dbo].[Purchase_Overview] WHERE Id = [ad].[ReferenceId])
		WHEN [ad].[ReferenceType] = 'PurchaseReturn' THEN
			(SELECT TransactionDateTime FROM [dbo].[PurchaseReturn_Overview] WHERE Id = [ad].[ReferenceId])
	END) AS ReferenceDateTime,

	(CASE
		WHEN [ad].[ReferenceType] = 'Sale' THEN
			(SELECT TotalAmount FROM [dbo].[Sale_Overview] WHERE Id = [ad].[ReferenceId])
		WHEN [ad].[ReferenceType] = 'SaleReturn' THEN
			(SELECT TotalAmount FROM [dbo].[SaleReturn_Overview] WHERE Id = [ad].[ReferenceId])
		WHEN [ad].[ReferenceType] = 'Purchase' THEN
			(SELECT TotalAmount FROM [dbo].[Purchase_Overview] WHERE Id = [ad].[ReferenceId])
		WHEN [ad].[ReferenceType] = 'PurchaseReturn' THEN
			(SELECT TotalAmount FROM [dbo].[PurchaseReturn_Overview] WHERE Id = [ad].[ReferenceId])
	END) AS ReferenceAmount,

	[ad].[Debit] AS Debit,
	[ad].[Credit] AS Credit,

	[ad].[Remarks]

FROM
	[dbo].[AccountingDetail] ad

INNER JOIN
	[dbo].[Accounting] a ON ad.AccountingId = a.Id
INNER JOIN
	[dbo].[Ledger] l ON ad.LedgerId = l.Id
INNER JOIN
	[dbo].[AccountType] at ON l.AccountTypeId = at.Id
INNER JOIN
	[dbo].[Group] g ON l.GroupId = g.Id
INNER JOIN
	[dbo].[Company] c ON a.CompanyId = c.Id

WHERE
	[a].[Status] = 1 AND
	[ad].[Status] = 1

GROUP BY
	[l].[Id],
	[l].[Name],
	[l].[Code],
	[l].[AccountTypeId],
	[at].[Name],
	[l].[GroupId],
	[g].[Name],
	[a].[Id],
	[a].[TransactionNo],
	[a].[TransactionDateTime],
	[a].[CompanyId],
	[c].[Name],
	[a].[Remarks],
	[ad].[ReferenceId],
	[ad].[ReferenceType],
	[ad].[ReferenceNo],
	[ad].[Debit],
	[ad].[Credit],
	[ad].[Remarks]