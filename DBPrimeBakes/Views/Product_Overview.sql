CREATE VIEW [dbo].[Product_Overview]
	AS
SELECT
	[p].Id AS ProductId,
	[p].[Name] AS ProductName,
	[p].[Code] AS ProductCode,
	[pc].[Id] AS ProductCategoryId,
	[pc].[Name] AS ProductCategoryName,
	[s].[Id] AS SaleId,
	[s].[SaleDateTime] AS BillDateTime,
	[s].[LocationId] AS LocationId,

	SUM([sd].[Quantity]) AS QuantitySold,
	AVG([sd].[Rate]) AS AveragePrice,

	SUM([sd].[BaseTotal]) AS BaseTotal,
	SUM([sd].[DiscAmount]) AS DiscountAmount,
	SUM([sd].[AfterDiscount]) AS SubTotal,

	SUM([sd].[CGSTAmount]) AS CGSTAmount,
	SUM([sd].[SGSTAmount]) AS SGSTAmount,
	SUM([sd].[IGSTAmount]) AS IGSTAmount,

	SUM([sd].[CGSTAmount] + [sd].[SGSTAmount] + [sd].[IGSTAmount]) AS TotalTaxAmount,

	SUM([sd].[Total]) AS TotalAmount

FROM
	[dbo].[SaleDetail] sd

INNER JOIN
	[dbo].[Sale] s ON sd.SaleId = s.Id
INNER JOIN
	[dbo].[Product] p ON sd.ProductId = p.Id
INNER JOIN
	[dbo].[ProductCategory] pc ON p.ProductCategoryId = pc.Id

WHERE
	[s].[Status] = 1 AND
	[sd].[Status] = 1

GROUP BY
	[p].Id,
	[p].[Name],
	[p].[Code],
	[pc].[Id],
	[pc].[Name],
	[s].[Id],
	[s].[SaleDateTime],
	[s].[LocationId];