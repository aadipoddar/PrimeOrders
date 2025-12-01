CREATE VIEW [dbo].[StockTransfer_Item_Overview]
	AS
SELECT
	[pr].[Id],
	[pr].[Name] AS ItemName,
	[pr].[Code] AS ItemCode,
	[pc].[Id] AS ItemCategoryId,
	[pc].[Name] AS ItemCategoryName,

	[s].[Id] AS MasterId,
	[s].[TransactionNo],
	[s].[TransactionDateTime],
	[c].[Id] AS CompanyId,
	[c].[Name] AS CompanyName,

	[fl].[Id] AS LocationId,
	[fl].[Name] AS LocationName,

	[tl].[Id] AS ToLocationId,
	[tl].[Name] AS ToLocationName,
	
	[s].[Remarks] AS StockTransferRemarks,

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

	[sd].[Remarks]

FROM
	[dbo].[StockTransferDetail] sd

INNER JOIN
	[dbo].[StockTransfer] s ON sd.[MasterId] = s.Id
INNER JOIN
	[dbo].[Product] pr ON sd.ProductId = pr.Id
INNER JOIN
	[dbo].[ProductCategory] pc ON pr.ProductCategoryId = pc.Id
INNER JOIN
	[dbo].[Company] c ON s.CompanyId = c.Id
INNER JOIN
	[dbo].[Location] fl ON s.[LocationId] = fl.Id
INNER JOIN
	[dbo].[Location] tl ON s.ToLocationId = tl.Id

WHERE
	[s].[Status] = 1 AND
	[sd].[Status] = 1;