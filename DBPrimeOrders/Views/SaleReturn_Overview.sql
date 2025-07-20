CREATE VIEW [dbo].[SaleReturn_Overview]
	AS
	SELECT
		sr.Id AS SaleReturnId,
		sr.TransactionNo,
		sr.SaleId,
		s.BillNo AS OriginalBillNo,
		sr.UserId,
		u.Name AS UserName,
		sr.LocationId,
		l.Name AS LocationName,
		sr.ReturnDateTime,
		sr.Remarks,

		COUNT(DISTINCT srd.Id) AS TotalProducts,
		SUM(srd.Quantity) AS TotalQuantity,

		AVG(srd.SGSTPercent) AS SGSTPercent,
		AVG(srd.CGSTPercent) AS CGSTPercent,
		AVG(srd.IGSTPercent) AS IGSTPercent,

		SUM(srd.SGSTAmount) AS SGSTAmount,
		SUM(srd.CGSTAmount) AS CGSTAmount,
		SUM(srd.IGSTAmount) AS IGSTAmount,

		SUM(srd.DiscAmount) AS DiscountAmount,
		SUM(srd.SGSTAmount + srd.CGSTAmount + srd.IGSTAmount) AS TotalTaxAmount,

		SUM(srd.BaseTotal) AS BaseTotal,
		SUM(srd.AfterDiscount) AS SubTotal,
		SUM(srd.Total) AS Total

	FROM
		dbo.SaleReturn sr

	INNER JOIN
		dbo.Sale s ON sr.SaleId = s.Id
	INNER JOIN
		dbo.Location l ON sr.LocationId = l.Id
	INNER JOIN
		dbo.[User] u ON sr.UserId = u.Id
	INNER JOIN
		dbo.SaleReturnDetail srd ON sr.Id = srd.SaleReturnId

	WHERE
		sr.Status = 1
		AND srd.Status = 1

	GROUP BY
		sr.Id,
		sr.TransactionNo,
		sr.SaleId,
		s.BillNo,
		sr.UserId,
		u.Name,
		sr.LocationId,
		l.Name,
		sr.ReturnDateTime,
		sr.Remarks,
		sr.Status