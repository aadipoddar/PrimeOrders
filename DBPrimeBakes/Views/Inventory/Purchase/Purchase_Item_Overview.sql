CREATE VIEW [dbo].[Purchase_Item_Overview]
	AS
SELECT
	[r].[Id],
	[r].[Name] AS ItemName,
	[r].[Code] AS ItemCode,
	[rc].[Id] AS ItemCategoryId,
	[rc].[Name] AS ItemCategoryName,

	[p].[Id] AS MasterId,
	[p].[TransactionNo],
	[p].[TransactionDateTime],
	[c].[Id] AS CompanyId,
	[c].[Name] AS CompanyName,
	[l].[Id] AS PartyId,
	[l].[Name] AS PartyName,
	[p].[Remarks] AS PurchaseRemarks,

	[pd].[Quantity],
	[pd].[Rate],
	[pd].[BaseTotal],

	[pd].[DiscountPercent],
	[pd].[DiscountAmount],
	[pd].[AfterDiscount],

	[pd].[CGSTPercent],
	[pd].[CGSTAmount],
	[pd].[SGSTPercent],
	[pd].[SGSTAmount],
	[pd].[IGSTPercent],
	[pd].[IGSTAmount],
	[pd].[TotalTaxAmount],
	[pd].[InclusiveTax],

	[pd].[Total],
	[pd].[NetRate],
	[pd].[NetRate] * [pd].[Quantity] AS NetTotal,

	[pd].[Remarks] AS Remarks

FROM
	[dbo].[PurchaseDetail] pd

INNER JOIN
	[dbo].[Purchase] p ON pd.[MasterId] = p.Id
INNER JOIN
	[dbo].[RawMaterial] r ON pd.RawMaterialId = r.Id
INNER JOIN
	[dbo].[RawMaterialCategory] rc ON r.RawMaterialCategoryId = rc.Id
INNER JOIN
	[dbo].[Company] c ON p.CompanyId = c.Id
INNER JOIN
	[dbo].[Ledger] l ON p.PartyId = l.Id

WHERE
	[p].[Status] = 1 AND
	[pd].[Status] = 1;