CREATE VIEW [dbo].[Sale_Overview]
	AS
SELECT
	[s].[Id],
	[s].[TransactionNo],
	[s].[CompanyId],
	[c].[Name] AS CompanyName,
	[s].[LocationId],
	[l].[Name] AS LocationName,

	[s].[PartyId],
	[p].[Name] AS PartyName,
	[s].[CustomerId],
	[cust].[Name] AS CustomerName,

	[s].[OrderId],
	[o].[TransactionNo] AS OrderTransactionNo,
	[o].[TransactionDateTime] AS OrderDateTime,

	[s].[TransactionDateTime],
	[s].[FinancialYearId],
	CONVERT(VARCHAR(10), fy.StartDate, 103) + ' to ' + CONVERT(VARCHAR(10), fy.EndDate, 103) AS FinancialYear,

	[s].[TotalItems],
	[s].[TotalQuantity],
	[s].[BaseTotal],
	[s].[ItemDiscountAmount],
	[s].[TotalAfterItemDiscount],
	[s].[TotalInclusiveTaxAmount],
	[s].[TotalExtraTaxAmount],
	[s].[TotalAfterTax],
	
	[s].[OtherChargesPercent],
	[s].[OtherChargesAmount],
	[s].[DiscountPercent],
	[s].[DiscountAmount],

	[s].[RoundOffAmount],
	[s].[TotalAmount],

	[s].[Cash],
	[s].[Card],
	[s].[UPI],
	[s].[Credit],

	STUFF(
		CONCAT(
			CASE WHEN [s].[Cash] > 0 THEN ',Cash' ELSE '' END,
			CASE WHEN [s].[Card] > 0 THEN ',Card' ELSE '' END,
			CASE WHEN [s].[UPI] > 0 THEN ',UPI' ELSE '' END,
			CASE WHEN [s].[Credit] > 0 THEN ',Credit' ELSE '' END
		), 1, 1, ''
	) AS PaymentModes,

	[s].[Remarks],
	[s].[CreatedBy],
	[u].[Name] AS CreatedByName,
	[s].[CreatedAt],
	[s].[CreatedFromPlatform],
	[s].[LastModifiedBy],
	[lm].[Name] AS LastModifiedByUserName,
	[s].[LastModifiedAt],
	[s].[LastModifiedFromPlatform],

	[s].[Status]

FROM
	[dbo].[Sale] AS s
INNER JOIN
	[dbo].[Company] AS c ON s.CompanyId = c.Id
INNER JOIN
	[dbo].[Location] AS l ON s.LocationId = l.Id
LEFT JOIN
	[dbo].[Ledger] AS p ON s.PartyId = p.Id
LEFT JOIN
	[dbo].[Customer] AS cust ON s.CustomerId = cust.Id
LEFT JOIN
	[dbo].[Order] AS o ON s.OrderId = o.Id
INNER JOIN
	[dbo].[FinancialYear] AS fy ON s.FinancialYearId = fy.Id
INNER JOIN
	[dbo].[User] AS u ON s.CreatedBy = u.Id
LEFT JOIN
	[dbo].[User] AS lm ON s.LastModifiedBy = lm.Id