CREATE VIEW [dbo].[SaleReturn_Overview]
	AS
SELECT
	[sr].[Id],
	[sr].[TransactionNo],
	[sr].[CompanyId],
	[c].[Name] AS CompanyName,
	[sr].[LocationId],
	[l].[Name] AS LocationName,
	
	[sr].[PartyId],
	[p].[Name] AS PartyName,
	[sr].[CustomerId],
	[cust].[Name] AS CustomerName,
	[sr].[TransactionDateTime],
	[sr].[FinancialYearId],
	CONVERT(VARCHAR(10), fy.StartDate, 103) + ' to ' + CONVERT(VARCHAR(10), fy.EndDate, 103) AS FinancialYear,
	
	[sr].[OtherChargesPercent],
	[sr].[OtherChargesAmount],
	[sr].[DiscountPercent],
	[sr].[DiscountAmount],

	COUNT(DISTINCT sd.Id) AS TotalItems,
	SUM(sd.Quantity) AS TotalQuantity,

	SUM(sd.BaseTotal) AS BaseTotal,

	AVG(sd.DiscountPercent) AS ItemDiscountPercent,
	SUM(sd.DiscountAmount) AS ItemDiscountAmount,

	SUM(sd.AfterDiscount) AS AfterDiscount,

	AVG(sd.SGSTPercent) AS SGSTPercent,
	AVG(sd.CGSTPercent) AS CGSTPercent,
	AVG(sd.IGSTPercent) AS IGSTPercent,

	SUM(sd.SGSTAmount) AS SGSTAmount,
	SUM(sd.CGSTAmount) AS CGSTAmount,
	SUM(sd.IGSTAmount) AS IGSTAmount,

	SUM(sd.TotalTaxAmount) AS TotalTaxAmount,
	
	SUM(sd.Total) AS TotalAfterTax,

	SUM(sd.Total) + [sr].[OtherChargesAmount] AS TotalAfterOtherCharges,
	SUM(sd.Total) + [sr].[OtherChargesAmount] - [sr].[DiscountAmount] AS TotalAfterDiscount,
	[sr].[RoundOffAmount],
	[sr].[TotalAmount],

	[sr].[Cash],
	[sr].[Card],
	[sr].[UPI],
	[sr].[Credit],

	[sr].[Remarks],
	[sr].[CreatedBy],
	[u].[Name] AS CreatedByName,
	[sr].[CreatedAt],
	[sr].[CreatedFromPlatform],
	[sr].[LastModifiedBy],
	[lm].[Name] AS LastModifiedByUserName,
	[sr].[LastModifiedAt],
	[sr].[LastModifiedFromPlatform],

	[sr].[Status]

FROM
	[dbo].[SaleReturn] AS sr
INNER JOIN
	[dbo].[Company] AS c ON sr.CompanyId = c.Id
INNER JOIN
	[dbo].[Location] AS l ON sr.LocationId = l.Id
LEFT JOIN
	[dbo].[Ledger] AS p ON sr.PartyId = p.Id
LEFT JOIN
	[dbo].[Customer] AS cust ON sr.CustomerId = cust.Id
INNER JOIN
	[dbo].[FinancialYear] AS fy ON sr.FinancialYearId = fy.Id
INNER JOIN
	[dbo].[User] AS u ON sr.CreatedBy = u.Id
LEFT JOIN
	[dbo].[User] AS lm ON sr.LastModifiedBy = lm.Id
INNER JOIN
	[dbo].[SaleReturnDetail] AS sd ON sr.Id = sd.SaleReturnId

WHERE
	sd.Status = 1

GROUP BY
	[sr].[Id],
	[sr].[TransactionNo],
	[sr].[CompanyId],
	[c].[Name],
	[sr].[LocationId],
	[l].[Name],
	[sr].[PartyId],
	[p].[Name],
	[sr].[CustomerId],
	[cust].[Name],
	[sr].[TransactionDateTime],
	[sr].[FinancialYearId],
	fy.StartDate,
	fy.EndDate,
	[sr].[OtherChargesPercent],
	[sr].[OtherChargesAmount],
	[sr].[DiscountPercent],
	[sr].[DiscountAmount],
	[sr].[RoundOffAmount],
	[sr].[TotalAmount],
	[sr].[Cash],
	[sr].[Card],
	[sr].[UPI],
	[sr].[Credit],
	[sr].[Remarks],
	[sr].[CreatedBy],
	[u].[Name],
	[sr].[CreatedAt],
	[sr].[CreatedFromPlatform],
	[sr].[LastModifiedBy],
	[lm].[Name],
	[sr].[LastModifiedAt],
	[sr].[LastModifiedFromPlatform],
	[sr].[Status]