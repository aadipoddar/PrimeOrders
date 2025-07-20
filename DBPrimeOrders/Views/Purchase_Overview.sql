CREATE VIEW [dbo].[Purchase_Overview]
	AS
SELECT
	[p].[Id] AS PurchaseId,
	[p].[UserId] AS UserId,
	[u].[Name] AS UserName,
	[s].[Id] AS SupplierId,
	[s].[Name] AS SupplierName,
	[p].[BillNo],
	[p].[BillDate],
	[p].[Remarks],
	[p].[CDPercent] AS CashDiscountPercent,
	[p].[CDAmount]AS CashDiscountAmount,

	COUNT(DISTINCT pd.Id) AS TotalItems,
	SUM(pd.Quantity) AS TotalQuantity,

	AVG(pd.SGSTPercent) AS SGSTPercent,
	AVG(pd.CGSTPercent) AS CGSTPercent,
	AVG(pd.IGSTPercent) AS IGSTPercent,

	SUM(pd.SGSTAmount) AS SGSTAmount,
	SUM(pd.CGSTAmount) AS CGSTAmount,
	SUM(pd.IGSTAmount) AS IGSTAmount,

	SUM(pd.DiscAmount) AS DiscountAmount,
	SUM(pd.SGSTAmount + pd.CGSTAmount + pd.IGSTAmount) AS TotalTaxAmount,

	SUM(pd.BaseTotal) AS BaseTotal,
	SUM(pd.AfterDiscount) AS SubTotal,

	SUM(pd.Total - p.CDAmount) AS Total

FROM
	[dbo].[Purchase] AS p
INNER JOIN
	[dbo].[User] AS u ON p.UserId = u.Id
INNER JOIN
	[dbo].[Ledger] AS s ON p.SupplierId = s.Id
INNER JOIN
	[dbo].[PurchaseDetail] AS pd ON p.Id = pd.PurchaseId

WHERE
	p.Status = 1
	AND pd.Status = 1

GROUP BY
	p.Id,
	p.UserId,
	u.Name,
	s.Id,
	s.Name,
	p.BillNo,
	p.BillDate,
	p.Remarks,
	p.CDPercent,
	p.CDAmount