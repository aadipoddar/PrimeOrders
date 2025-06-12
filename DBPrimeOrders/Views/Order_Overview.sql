CREATE VIEW [dbo].[Order_Overview]
	AS
	SELECT
		[o].[Id] AS OrderId,
		[o].[OrderNo],
		[o].[UserId],
		[u].[Name] AS UserName,
		[o].[LocationId],
		[l].[Name] AS LocationName,
		[o].[OrderDate],
		[o].[Remarks],

		COUNT(DISTINCT od.Id) AS TotalProducts,
		SUM(od.Quantity) AS TotalQuantity,

		[o].[SaleId],
		[s].[BillNo] AS SaleBillNo,
		[s].[SaleDateTime]

	FROM
		[dbo].[Order] o
	INNER JOIN
		[dbo].[OrderDetail] od ON o.Id = od.OrderId
	INNER JOIN
		[dbo].[Location] l ON o.LocationId = l.Id
	INNER JOIN
		[dbo].[User] u ON o.UserId = u.Id
	LEFT JOIN
		[dbo].[Sale] s ON o.SaleId = s.Id

	WHERE
		[o].[Status] = 1
		AND [od].[Status] = 1

	GROUP BY
		[o].[Id],
		[o].[OrderNo],
		[o].[UserId],
		[u].[Name],
		[o].[LocationId],
		[l].[Name],
		[o].[OrderDate],
		[o].[Remarks],
		[o].[SaleId],
		[s].[BillNo],
		[s].[SaleDateTime]