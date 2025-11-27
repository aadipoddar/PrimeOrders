CREATE VIEW [dbo].[KitchenIssue_Overview]
	AS
	SELECT
		[ki].[Id],
		[ki].[TransactionNo],
		[ki].[CompanyId],
		[c].[Name] AS CompanyName,
		[ki].[TransactionDateTime],
		[ki].[FinancialYearId],
		CONVERT(VARCHAR(10), fy.StartDate, 103) + ' to ' + CONVERT(VARCHAR(10), fy.EndDate, 103) AS FinancialYear,
		
		[ki].[KitchenId],
		[k].[Name] AS KitchenName,

		[ki].[TotalItems],
		[ki].[TotalQuantity],
		[ki].[TotalAmount],
		
		[ki].[Remarks],
		[ki].[CreatedBy],
		[u].[Name] AS CreatedByName,
		[ki].[CreatedAt],
		[ki].[CreatedFromPlatform],
		[ki].[LastModifiedBy],
		[lm].[Name] AS LastModifiedByUserName,
		[ki].[LastModifiedAt],
		[ki].[LastModifiedFromPlatform],

		[ki].[Status]

	FROM
		dbo.KitchenIssue ki
	INNER JOIN
		[dbo].[Company] AS c ON ki.CompanyId = c.Id
	INNER JOIN
		[dbo].[FinancialYear] AS fy ON ki.FinancialYearId = fy.Id
	INNER JOIN
		dbo.Kitchen k ON ki.KitchenId = k.Id
	INNER JOIN
		[dbo].[User] AS u ON ki.[CreatedBy] = u.Id
	LEFT JOIN
		[dbo].[User] AS lm ON ki.LastModifiedBy = lm.Id