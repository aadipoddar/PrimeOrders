CREATE VIEW [dbo].[PurchaseReturn_Overview]
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
	
	[p].[OtherChargesPercent],
	[p].[OtherChargesAmount],
	[p].[CashDiscountPercent],
	[p].[CashDiscountAmount],

	COUNT(DISTINCT pd.Id) AS TotalItems,
	SUM(pd.Quantity) AS TotalQuantity,

	SUM(pd.BaseTotal) AS BaseTotal,

	AVG(pd.DiscountPercent) AS DiscountPercent,
	SUM(pd.DiscountAmount) AS DiscountAmount,

	SUM(pd.AfterDiscount) AS AfterDiscount,

	AVG(pd.SGSTPercent) AS SGSTPercent,
	AVG(pd.CGSTPercent) AS CGSTPercent,
	AVG(pd.IGSTPercent) AS IGSTPercent,

	SUM(pd.SGSTAmount) AS SGSTAmount,
	SUM(pd.CGSTAmount) AS CGSTAmount,
	SUM(pd.IGSTAmount) AS IGSTAmount,

	SUM(pd.TotalTaxAmount) AS TotalTaxAmount,
	
	SUM(pd.Total) AS TotalAfterTax,

	SUM(pd.Total) + [p].[OtherChargesAmount] AS TotalAfterOtherCharges,
	SUM(pd.Total) + [p].[OtherChargesAmount] - [p].[CashDiscountAmount] AS TotalAfterCashDiscount,
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
	[p].[LastModifiedFromPlatform]

FROM
	[dbo].[PurchaseReturn] AS p
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
INNER JOIN
	[dbo].[PurchaseReturnDetail] AS pd ON p.Id = pd.PurchaseReturnId

WHERE
	p.Status = 1
	AND pd.Status = 1

GROUP BY
	[p].[Id],
	[p].[TransactionNo],
	[p].[CompanyId],
	[c].[Name],
	[p].[PartyId],
	[l].[Name],
	[p].[TransactionDateTime],
	[p].[FinancialYearId],
	fy.StartDate,
	fy.EndDate,
	[p].[OtherChargesPercent],
	[p].[OtherChargesAmount],
	[p].[CashDiscountPercent],
	[p].[CashDiscountAmount],
	[p].[RoundOffAmount],
	[p].[TotalAmount],
	[p].[Remarks],
	[p].[DocumentUrl],
	[p].[CreatedBy],
	[u].[Name],
	[p].[CreatedAt],
	[p].[CreatedFromPlatform],
	[p].[LastModifiedBy],
	[lm].[Name],
	[p].[LastModifiedAt],
	[p].[LastModifiedFromPlatform]