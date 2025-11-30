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
	
	[sr].[TotalItems],
	[sr].[TotalQuantity],
	[sr].[BaseTotal],
	[sr].[ItemDiscountAmount],
	[sr].[TotalAfterItemDiscount],
	[sr].[TotalInclusiveTaxAmount],
	[sr].[TotalExtraTaxAmount],
	[sr].[TotalAfterTax],

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

	STUFF(
		CONCAT(
			CASE WHEN [sr].[Cash] > 0 THEN ',Cash' ELSE '' END,
			CASE WHEN [sr].[Card] > 0 THEN ',Card' ELSE '' END,
			CASE WHEN [sr].[UPI] > 0 THEN ',UPI' ELSE '' END,
			CASE WHEN [sr].[Credit] > 0 THEN ',Credit' ELSE '' END
		), 1, 1, ''
	) AS PaymentModes,

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