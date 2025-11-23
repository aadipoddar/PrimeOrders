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

		COUNT([od].[Id]) AS TotalItems,
		SUM([od].[Quantity]) AS TotalQuantity,

		[o].[TransactionDateTime],
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
		[dbo].[OrderDetail] od ON o.Id = od.OrderId
	INNER JOIN
		[dbo].[User] AS u ON s.CreatedBy = u.Id
	LEFT JOIN
		[dbo].[User] AS lm ON s.LastModifiedBy = lm.Id

	WHERE
		[o].[Status] = 1
		AND [od].[Status] = 1

	GROUP BY
		[o].[Id],
		[o].[TransactionNo],
		[o].[CompanyId],
		[c].[Name],
		[o].[LocationId],
		[l].[Name],
		[o].[SaleId],
		[s].[TransactionNo],
		[s].[TransactionDateTime],
		[o].[TransactionDateTime],
		[o].[Remarks],
		[o].[CreatedBy],
		[u].[Name],
		[o].[CreatedAt],
		[o].[CreatedFromPlatform],
		[o].[LastModifiedBy],
		[lm].[Name],
		[o].[LastModifiedAt],
		[o].[LastModifiedFromPlatform],
		[o].[Status]