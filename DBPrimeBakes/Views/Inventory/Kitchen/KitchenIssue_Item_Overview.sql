CREATE VIEW [dbo].[KitchenIssue_Item_Overview]
	AS
SELECT
	[r].[Id],
	[r].[Name] AS ItemName,
	[r].[Code] AS ItemCode,
	[rc].[Id] AS ItemCategoryId,
	[rc].[Name] AS ItemCategoryName,

	[ki].[Id] AS MasterId,
	[ki].[TransactionNo],
	[ki].[TransactionDateTime],
	[c].[Id] AS CompanyId,
	[c].[Name] AS CompanyName,
	[k].[Id] AS KitchenId,
	[k].[Name] AS KitchenName,
	[ki].[Remarks] AS KitchenIssueRemarks,

	[kid].[Quantity],
	[kid].[Rate],
	[kid].[Total],

	[kid].[Remarks] AS Remarks

FROM
	[dbo].[KitchenIssueDetail] kid

INNER JOIN
	[dbo].[KitchenIssue] ki ON kid.[MasterId] = ki.Id
INNER JOIN
	[dbo].[RawMaterial] r ON kid.RawMaterialId = r.Id
INNER JOIN
	[dbo].[RawMaterialCategory] rc ON r.RawMaterialCategoryId = rc.Id
INNER JOIN
	[dbo].[Company] c ON ki.CompanyId = c.Id
INNER JOIN
	[dbo].[Kitchen] k ON ki.KitchenId = k.Id

WHERE
	[ki].[Status] = 1 AND
	[kid].[Status] = 1;