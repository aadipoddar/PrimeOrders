CREATE PROCEDURE [dbo].[Load_ProductStockSummary_By_Date_LocationId]
	@FromDate DATE,
	@ToDate DATE,
	@LocationId INT
AS
BEGIN
	SET NOCOUNT ON;

	-- Pre-calculate all stock aggregations in a single pass for each product
	WITH StockAggregates AS (
		SELECT 
			ProductId,
			-- Opening Stock: sum of all quantities before FromDate
			SUM(CASE WHEN TransactionDate < @FromDate THEN Quantity ELSE 0 END) AS OpeningStock,
			
			-- Purchase Stock: sum of positive quantities in date range
			SUM(CASE WHEN TransactionDate >= @FromDate AND TransactionDate <= @ToDate AND Quantity > 0 
				THEN Quantity ELSE 0 END) AS PurchaseStock,
			
			-- Sale Stock: sum of negative quantities in date range
			SUM(CASE WHEN TransactionDate >= @FromDate AND TransactionDate <= @ToDate AND Quantity < 0 
				THEN Quantity ELSE 0 END) AS SaleStock,
			
			-- Monthly Stock: sum of all quantities in date range
			SUM(CASE WHEN TransactionDate >= @FromDate AND TransactionDate <= @ToDate 
				THEN Quantity ELSE 0 END) AS MonthlyStock,
			
			-- Closing Stock: sum of all quantities up to ToDate
			SUM(CASE WHEN TransactionDate <= @ToDate THEN Quantity ELSE 0 END) AS ClosingStock
		FROM [ProductStock] WITH (NOLOCK)
		WHERE LocationId = @LocationId
		GROUP BY ProductId
	),
	-- Calculate average and weighted average prices for sales in date range
	PriceAggregates AS (
		SELECT 
			ProductId,
			AVG(CASE WHEN Quantity < 0 AND NetRate IS NOT NULL THEN NetRate ELSE NULL END) AS AveragePrice
		FROM [ProductStock] WITH (NOLOCK)
		WHERE LocationId = @LocationId
			AND TransactionDate >= @FromDate 
			AND TransactionDate <= @ToDate
		GROUP BY ProductId
	),
	-- Get last sale price and date for each product in date range
	LastSaleInfo AS (
		SELECT 
			ProductId,
			NetRate AS LastSalePrice,
			ROW_NUMBER() OVER (PARTITION BY ProductId ORDER BY TransactionDate DESC, Id DESC) AS RowNum
		FROM [ProductStock] WITH (NOLOCK)
		WHERE LocationId = @LocationId
			AND TransactionDate >= @FromDate 
			AND TransactionDate <= @ToDate
			AND Quantity < 0
			AND NetRate IS NOT NULL
	),
	-- Get product location rate
	ProductLocationRate AS (
		SELECT 
			ProductId,
			Rate,
			ROW_NUMBER() OVER (PARTITION BY ProductId ORDER BY Id DESC) AS RowNum
		FROM dbo.ProductLocation WITH (NOLOCK)
		WHERE LocationId = @LocationId
			AND Status = 1
	)
	-- Final select combining all pre-calculated data
	SELECT
		sa.ProductId,
		p.[Name] AS ProductName,
		p.Code AS ProductCode,
		p.ProductCategoryId,
		pc.[Name] ProductCategoryName,
		
		ISNULL(sa.OpeningStock, 0) AS OpeningStock,
		ISNULL(sa.PurchaseStock, 0) AS PurchaseStock,
		ISNULL(sa.SaleStock, 0) AS SaleStock,
		ISNULL(sa.MonthlyStock, 0) AS MonthlyStock,
		ISNULL(sa.ClosingStock, 0) AS ClosingStock,
		
		ISNULL(plr.Rate, p.Rate) AS Rate,
		ISNULL(ISNULL(plr.Rate, p.Rate) * sa.ClosingStock, 0) AS ClosingValue,
		
		ISNULL(pa.AveragePrice, 0) AS AveragePrice,
		ISNULL(pa.AveragePrice * sa.ClosingStock, 0) AS WeightedAverageValue,
		
		ISNULL(lsi.LastSalePrice, 0) AS LastSalePrice,
		ISNULL(lsi.LastSalePrice * sa.ClosingStock, 0) AS LastSaleValue
		
	FROM StockAggregates sa
	
	LEFT JOIN dbo.Product p WITH (NOLOCK)
		ON p.Id = sa.ProductId
	
	LEFT JOIN dbo.ProductCategory pc WITH (NOLOCK) 
		ON pc.Id = p.ProductCategoryId
		
	LEFT JOIN PriceAggregates pa 
		ON pa.ProductId = p.Id
		
	LEFT JOIN LastSaleInfo lsi 
		ON lsi.ProductId = p.Id AND lsi.RowNum = 1
		
	LEFT JOIN ProductLocationRate plr 
		ON plr.ProductId = sa.ProductId AND plr.RowNum = 1
		
	WHERE sa.OpeningStock != 0 
		OR sa.PurchaseStock != 0 
		OR sa.SaleStock != 0 
		OR sa.ClosingStock != 0;
END