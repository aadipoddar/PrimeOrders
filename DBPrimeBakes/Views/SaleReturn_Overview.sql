CREATE VIEW [dbo].[SaleReturn_Overview]
	AS
	SELECT
		[s].[Id] AS SaleReturnId,
		[s].[BillNo],
		[s].[UserId],
		[u].[Name] AS UserName,
		[s].[LocationId],
		[l].[Name] AS LocationName,
		[s].[SaleReturnDateTime],
		[s].[PartyId],
		[p].[Name] AS PartyName,
		[s].[Remarks],
		[s].[DiscPercent] AS DiscountPercent,
		[s].[DiscReason] AS DiscountReason,

		COUNT(DISTINCT sd.Id) AS TotalProducts,
		SUM(sd.Quantity) AS TotalQuantity,

		AVG(sd.SGSTPercent) AS SGSTPercent,
		AVG(sd.CGSTPercent) AS CGSTPercent,
		AVG(sd.IGSTPercent) AS IGSTPercent,

		SUM(sd.SGSTAmount) AS SGSTAmount,
		SUM(sd.CGSTAmount) AS CGSTAmount,
		SUM(sd.IGSTAmount) AS IGSTAmount,

		SUM(sd.DiscAmount) AS DiscountAmount,
		SUM(sd.SGSTAmount + sd.CGSTAmount + sd.IGSTAmount) AS TotalTaxAmount,

		SUM(sd.BaseTotal) AS BaseTotal,
		SUM(sd.AfterDiscount) AS SubTotal,
		SUM(sd.Total) AS AfterTax,
		[s].[RoundOff],
		SUM(sd.Total) + [s].[RoundOff] AS Total,

		[s].[Cash],
		[s].[Card],
		[s].[UPI],
		[s].[Credit],
		[s].[CustomerId],
		[c].[Name] AS CustomerName,
		[c].[Number] AS CustomerNumber,
		[s].[CreatedAt]

	FROM
		[dbo].[SaleReturn] s
	INNER JOIN
		[dbo].[SaleReturnDetail] sd ON s.Id = sd.SaleReturnId
	INNER JOIN
		[dbo].[Location] l ON s.LocationId = l.Id
	INNER JOIN
		[dbo].[User] u ON s.UserId = u.Id
	LEFT JOIN
		[dbo].[Ledger] p ON s.PartyId = p.Id
	LEFT JOIN
		[dbo].[Customer] c ON s.CustomerId = c.Id

	WHERE
		[s].[Status] = 1
		AND [sd].[Status] = 1

	GROUP BY
		[s].[Id],
		[s].[BillNo],
		[s].[UserId],
		[u].[Name],
		[s].[LocationId],
		[l].[Name],
		[s].[SaleReturnDateTime],
		[s].[PartyId],
		[p].[Name],
		[s].[Remarks],
		[s].[DiscPercent],
		[s].[DiscReason],
		[s].[RoundOff],
		[s].[Cash],
		[s].[Card],
		[s].[UPI],
		[s].[Credit],
		[s].[Status],
		[s].[CustomerId],
		[c].[Name],
		[c].[Number],
		[s].[CreatedAt];