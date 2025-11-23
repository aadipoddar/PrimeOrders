CREATE VIEW [dbo].[Sale_Item_Overview]
	AS
SELECT
	[pr].[Id],
	[pr].[Name] AS ItemName,
	[pr].[Code] AS ItemCode,
	[pc].[Id] AS ItemCategoryId,
	[pc].[Name] AS ItemCategoryName,

	[s].[Id] AS SaleId,
	[s].[TransactionNo],
	[s].[TransactionDateTime],
	[c].[Id] AS CompanyId,
	[c].[Name] AS CompanyName,
	[l].[Id] AS LocationId,
	[l].[Name] AS LocationName,

	[p].[Id] AS PartyId,
	[p].[Name] AS PartyName,
	[cust].[Id] AS CustomerId,
	[cust].[Name] AS CustomerName,

	[s].[OrderId],
	[o].[TransactionNo] AS OrderTransactionNo,
	[s].[Remarks] AS SaleRemarks,

	[sd].[Quantity],
	[sd].[Rate],
	[sd].[BaseTotal],

	[sd].[DiscountPercent],
	[sd].[DiscountAmount],
	[sd].[AfterDiscount],

	[sd].[CGSTPercent],
	[sd].[CGSTAmount],
	[sd].[SGSTPercent],
	[sd].[SGSTAmount],
	[sd].[IGSTPercent],
	[sd].[IGSTAmount],
	[sd].[TotalTaxAmount],
	[sd].[InclusiveTax],

	[sd].[Total],
	[sd].[NetRate],

	[sd].[Remarks]

FROM
	[dbo].[SaleDetail] sd

INNER JOIN
	[dbo].[Sale] s ON sd.SaleId = s.Id
INNER JOIN
	[dbo].[Product] pr ON sd.ProductId = pr.Id
INNER JOIN
	[dbo].[ProductCategory] pc ON pr.ProductCategoryId = pc.Id
INNER JOIN
	[dbo].[Company] c ON s.CompanyId = c.Id
INNER JOIN
	[dbo].[Location] l ON s.LocationId = l.Id
LEFT JOIN
	[dbo].[Ledger] p ON s.PartyId = p.Id
LEFT JOIN
	[dbo].[Customer] cust ON s.CustomerId = cust.Id
LEFT JOIN
	[dbo].[Order_Overview] o ON s.OrderId = o.Id

WHERE
	[s].[Status] = 1 AND
	[sd].[Status] = 1;