CREATE VIEW	[dbo].[Order_Overview]
	AS
	SELECT
		[o].[Id],
		[o].[TransactionNo],

		[o].[CompanyId],
		[c].[Name] AS CompanyName,
		[o].[LocationId],
		[l].[Name] AS LocationName,

		[o].[SaleId],
		[s].[TransactionNo] AS SaleTransactionNo,
		[s].[TransactionDateTime] AS SaleDateTime,

		[o].[TransactionDateTime],
		[o].[FinancialYearId],
		CONVERT(VARCHAR(10), fy.StartDate, 103) + ' to ' + CONVERT(VARCHAR(10), fy.EndDate, 103) AS FinancialYear,

		[o].[TotalItems],
		[o].[TotalQuantity],

		[o].[Remarks],
		[o].[CreatedBy],
		[u].[Name] AS CreatedByName,
		[o].[CreatedAt],
		[o].[CreatedFromPlatform],
		[o].[LastModifiedBy],
		[lm].[Name] AS LastModifiedByUserName,
		[o].[LastModifiedAt],
		[o].[LastModifiedFromPlatform],
		[o].[Status]

	FROM
		[dbo].[Order] o

	INNER JOIN
		[dbo].[Company] c ON o.CompanyId = c.Id
	INNER JOIN
		[dbo].[Location] l ON o.LocationId = l.Id
	LEFT JOIN
		[dbo].[Sale] s ON o.SaleId = s.Id
	INNER JOIN
		[dbo].[FinancialYear] AS fy ON o.FinancialYearId = fy.Id
	INNER JOIN
		[dbo].[User] AS u ON o.CreatedBy = u.Id
	LEFT JOIN
		[dbo].[User] AS lm ON o.LastModifiedBy = lm.Id