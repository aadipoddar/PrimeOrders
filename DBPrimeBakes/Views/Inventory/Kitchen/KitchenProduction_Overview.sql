CREATE VIEW [dbo].[KitchenProduction_Overview]
	AS
	SELECT
		[kp].[Id],
		[kp].[TransactionNo],
		[kp].[CompanyId],
		[c].[Name] AS CompanyName,
		[kp].[TransactionDateTime],
		[kp].[FinancialYearId],
		CONVERT(VARCHAR(10), fy.StartDate, 103) + ' to ' + CONVERT(VARCHAR(10), fy.EndDate, 103) AS FinancialYear,
		
		[kp].[KitchenId],
		[k].[Name] AS KitchenName,

		[kp].[TotalItems],
		[kp].[TotalQuantity],
		[kp].[TotalAmount],
		
		[kp].[Remarks],
		[kp].[CreatedBy],
		[u].[Name] AS CreatedByName,
		[kp].[CreatedAt],
		[kp].[CreatedFromPlatform],
		[kp].[LastModifiedBy],
		[lm].[Name] AS LastModifiedByUserName,
		[kp].[LastModifiedAt],
		[kp].[LastModifiedFromPlatform],

		[kp].[Status]

	FROM
		dbo.KitchenProduction kp
	INNER JOIN
		[dbo].[Company] AS c ON kp.CompanyId = c.Id
	INNER JOIN
		[dbo].[FinancialYear] AS fy ON kp.FinancialYearId = fy.Id
	INNER JOIN
		dbo.Kitchen k ON kp.KitchenId = k.Id
	INNER JOIN
		[dbo].[User] AS u ON kp.[CreatedBy] = u.Id
	LEFT JOIN
		[dbo].[User] AS lm ON kp.LastModifiedBy = lm.Id