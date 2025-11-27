CREATE VIEW [dbo].[KitchenProduction_Item_Overview]
	AS
SELECT
	[p].[Id],
	[p].[Name] AS ItemName,
	[p].[Code] AS ItemCode,
	[pc].[Id] AS ItemCategoryId,
	[pc].[Name] AS ItemCategoryName,

	[kp].[Id] AS MasterId,
	[kp].[TransactionNo],
	[kp].[TransactionDateTime],
	[c].[Id] AS CompanyId,
	[c].[Name] AS CompanyName,
	[k].[Id] AS KitchenId,
	[k].[Name] AS KitchenName,
	[kp].[Remarks] AS KitchenProductionRemarks,

	[kpd].[Quantity],
	[kpd].[Rate],
	[kpd].[Total],

	[kpd].[Remarks] AS Remarks

FROM
	[dbo].[KitchenProductionDetail] kpd

INNER JOIN
	[dbo].[KitchenProduction] kp ON kpd.[MasterId] = kp.Id
INNER JOIN
	[dbo].[Product] p ON kpd.ProductId = p.Id
INNER JOIN
	[dbo].[ProductCategory] pc ON p.ProductCategoryId = pc.Id
INNER JOIN
	[dbo].[Company] c ON kp.CompanyId = c.Id
INNER JOIN
	[dbo].[Kitchen] k ON kp.KitchenId = k.Id

WHERE
	[kp].[Status] = 1 AND
	[kpd].[Status] = 1;
