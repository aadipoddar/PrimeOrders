CREATE VIEW [dbo].[SaleReturn_Item_Overview]
	AS
SELECT
	[pr].[Id],
	[pr].[Name] AS ItemName,
	[pr].[Code] AS ItemCode,
	[pc].[Id] AS ItemCategoryId,
	[pc].[Name] AS ItemCategoryName,

	[sr].[Id] AS MasterId,
	[sr].[TransactionNo],
	[sr].[TransactionDateTime],
	[c].[Id] AS CompanyId,
	[c].[Name] AS CompanyName,
	[l].[Id] AS LocationId,
	[l].[Name] AS LocationName,

	[p].[Id] AS PartyId,
	[p].[Name] AS PartyName,
	[cust].[Id] AS CustomerId,
	[cust].[Name] AS CustomerName,
	[sr].[Remarks] AS SaleReturnRemarks,

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
	[sd].[NetRate] * [sd].[Quantity] AS NetTotal,

	[sd].[Remarks] AS Remarks

FROM
	[dbo].[SaleReturnDetail] sd

INNER JOIN
	[dbo].[SaleReturn] sr ON sd.[MasterId] = sr.Id
INNER JOIN
	[dbo].[Product] pr ON sd.ProductId = pr.Id
INNER JOIN
	[dbo].[ProductCategory] pc ON pr.ProductCategoryId = pc.Id
INNER JOIN
	[dbo].[Company] c ON sr.CompanyId = c.Id
INNER JOIN
	[dbo].[Location] l ON sr.LocationId = l.Id
LEFT JOIN
	[dbo].[Ledger] p ON sr.PartyId = p.Id
LEFT JOIN
	[dbo].[Customer] cust ON sr.CustomerId = cust.Id

WHERE
	[sr].[Status] = 1 AND
	[sd].[Status] = 1;