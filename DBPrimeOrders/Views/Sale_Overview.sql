﻿CREATE VIEW [dbo].[Sale_Overview]
	AS
	SELECT
		[s].[Id] AS SaleId,
		[s].[BillNo],
		[s].[UserId],
		[u].[Name] AS UserName,
		[s].[LocationId],
		[l].[Name] AS LocationName,
		[s].[SaleDateTime],
		[s].[PartyId],
		[p].[Name] AS PartyName,
		[s].[OrderId],
		[o].[OrderNo],
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
		SUM(sd.Total) AS Total,

		[s].[Cash],
		[s].[Card],
		[s].[UPI],
		[s].[Credit]
	FROM
		[dbo].[Sale] s
	INNER JOIN
		[dbo].[SaleDetail] sd ON s.Id = sd.SaleId
	INNER JOIN
		[dbo].[Location] l ON s.LocationId = l.Id
	INNER JOIN
		[dbo].[User] u ON s.UserId = u.Id
	LEFT JOIN
		[dbo].[Ledger] p ON s.PartyId = p.Id
	LEFT JOIN
		[dbo].[Order] o ON s.OrderId = o.Id

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
		[s].[SaleDateTime],
		[s].[PartyId],
		[p].[Name],
		[s].[OrderId],
		[o].[OrderNo],
		[s].[Remarks],
		[s].[DiscPercent],
		[s].[DiscReason],
		[s].[Cash],
		[s].[Card],
		[s].[UPI],
		[s].[Credit],
		[s].[Status];