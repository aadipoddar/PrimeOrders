CREATE VIEW [dbo].[Order_Item_Overview]
	AS
SELECT
	[pr].[Id],
	[pr].[Name] AS ItemName,
	[pr].[Code] AS ItemCode,
	[pc].[Id] AS ItemCategoryId,
	[pc].[Name] AS ItemCategoryName,

	[o].[Id] AS OrderId,
	[o].[TransactionNo],
	[o].[TransactionDateTime],
	[c].[Id] AS CompanyId,
	[c].[Name] AS CompanyName,
	[l].[Id] AS LocationId,
	[l].[Name] AS LocationName,

	[o].[SaleId],
	[s].[TransactionNo] AS SaleTransactionNo,
	[o].[Remarks] AS OrderRemarks,

	[od].[Quantity],
	[od].[Remarks]

FROM
	[dbo].[OrderDetail] od

INNER JOIN
	[dbo].[Order] o ON od.OrderId = o.Id
INNER JOIN
	[dbo].[Product] pr ON od.ProductId = pr.Id
INNER JOIN
	[dbo].[ProductCategory] pc ON pr.ProductCategoryId = pc.Id
INNER JOIN
	[dbo].[Company] c ON o.CompanyId = c.Id
INNER JOIN
	[dbo].[Location] l ON o.LocationId = l.Id
LEFT JOIN
	[dbo].[Sale] s ON o.SaleId = s.Id

WHERE
	[o].[Status] = 1 AND
	[od].[Status] = 1;