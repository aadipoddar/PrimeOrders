CREATE VIEW [dbo].[Purchase_Overview]
	AS
SELECT
	[p].[Id],
	[p].[TransactionNo],
	[p].[CompanyId],
	[c].[Name] AS CompanyName,
	[p].[PartyId],
	[l].[Name] AS PartyName,
	[p].[TransactionDateTime],
	[p].[FinancialYearId],
	CONVERT(VARCHAR(10), fy.StartDate, 103) + ' to ' + CONVERT(VARCHAR(10), fy.EndDate, 103) AS FinancialYear,

	[p].[TotalItems],
	[p].[TotalQuantity],
	[p].[BaseTotal],
	[p].[ItemDiscountAmount],
	[p].[TotalAfterItemDiscount],
	[p].[TotalInclusiveTaxAmount],
	[p].[TotalExtraTaxAmount],
	[p].[TotalAfterTax],

	[p].[OtherChargesPercent],
	[p].[OtherChargesAmount],
	[p].[CashDiscountPercent],
	[p].[CashDiscountAmount],

	[p].[RoundOffAmount],
	[p].[TotalAmount],

	[p].[Remarks],
	[p].[DocumentUrl],
	[p].[CreatedBy],
	[u].[Name] AS CreatedByName,
	[p].[CreatedAt],
	[p].[CreatedFromPlatform],
	[p].[LastModifiedBy],
	[lm].[Name] AS LastModifiedByUserName,
	[p].[LastModifiedAt],
	[p].[LastModifiedFromPlatform],

	[p].[Status]

FROM
	[dbo].[Purchase] AS p
INNER JOIN
	[dbo].[Company] AS c ON p.CompanyId = c.Id
INNER JOIN
	[dbo].[Ledger] AS l ON p.PartyId = l.Id
INNER JOIN
	[dbo].[FinancialYear] AS fy ON p.FinancialYearId = fy.Id
INNER JOIN
	[dbo].[User] AS u ON p.[CreatedBy] = u.Id
LEFT JOIN
	[dbo].[User] AS lm ON p.LastModifiedBy = lm.Id