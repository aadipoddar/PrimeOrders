CREATE PROCEDURE [dbo].[Load_ProductStockSummary_By_Date_LocationId]
	@FromDate DATE,
	@ToDate DATE,
	@LocationId INT
AS
BEGIN
	SELECT
        s.ProductId,
        p.[Name] AS ProductName,
        p.Code AS ProductCode,
        p.ProductCategoryId,
        pc.[Name] ProductCategoryName,

        ISNULL
        (
           (SELECT SUM (Quantity)
            FROM [ProductStock]
            WHERE     ProductId = s.ProductId
                  AND TransactionDate < @FromDate
                  AND LocationId = @LocationId),
           0) AS OpeningStock,

        ISNULL
        (
           (SELECT SUM (Quantity)
            FROM [ProductStock]
            WHERE     ProductId = s.ProductId
                  AND TransactionDate >= @FromDate
                  AND TransactionDate <= @ToDate
                  AND Quantity > 0
                  AND LocationId = @LocationId),
           0) AS PurchaseStock,

        ISNULL
        (
           (SELECT SUM (Quantity)
            FROM [ProductStock]
            WHERE     ProductId = s.ProductId
                  AND TransactionDate >= @FromDate
                  AND TransactionDate <= @ToDate
                  AND Quantity < 0
                  AND LocationId = @LocationId),
           0) AS SaleStock,

        ISNULL
        (
           (SELECT SUM (Quantity)
            FROM [ProductStock]
            WHERE     ProductId = s.ProductId
                  AND TransactionDate >= @FromDate
                  AND TransactionDate <= @ToDate
                  AND LocationId = @LocationId),
           0) AS MonthlyStock,

        ISNULL
        (
           (SELECT SUM (Quantity)
            FROM [ProductStock]
            WHERE     ProductId = s.ProductId
                  AND TransactionDate <= @ToDate
                  AND LocationId = @LocationId),
           0) AS ClosingStock,

        ISNULL
        (
            (SELECT TOP 1 Rate
                FROM dbo.ProductLocation
                WHERE ProductId = s.ProductId
                  AND LocationId = @LocationId
                  AND Status = 1)
         , p.Rate) AS Rate,

        ISNULL
        (
            ISNULL
            (
                (SELECT TOP 1 Rate
                    FROM dbo.ProductLocation
                    WHERE ProductId = s.ProductId
                      AND LocationId = @LocationId
                      AND Status = 1)
             , p.Rate) *
                ISNULL
                  (
                     (SELECT SUM (Quantity)
                      FROM [ProductStock]
                      WHERE     ProductId = s.ProductId
                            AND TransactionDate <= @ToDate
                            AND LocationId = @LocationId),
                     0)
        , 0) AS ClosingValue,

        ISNULL
        (
            (SELECT AVG (NetRate)
             FROM [ProductStock]
             WHERE     ProductId = s.ProductId
                   AND TransactionDate >= @FromDate
                   AND TransactionDate <= @ToDate
                   AND Quantity < 0
                   AND NetRate IS NOT NULL
                   AND LocationId = @LocationId),
        0) AS AveragePrice,

        ISNULL
        (
            (SELECT AVG (NetRate) * 
                (
                    ISNULL
                      (
                         (SELECT SUM (Quantity)
                          FROM [ProductStock]
                          WHERE     ProductId = s.ProductId
                                AND TransactionDate <= @ToDate
                                AND LocationId = @LocationId),
                         0)
                )
             FROM [ProductStock]
             WHERE     ProductId = s.ProductId
                   AND TransactionDate >= @FromDate
                   AND TransactionDate <= @ToDate
                   AND Quantity < 0
                   AND NetRate IS NOT NULL
                   AND LocationId = @LocationId),
        0) AS WeightedAverageValue,

        ISNULL
        (
            (SELECT TOP 1 NetRate
             FROM [ProductStock]
             WHERE     ProductId = s.ProductId
                   AND TransactionDate >= @FromDate
                   AND TransactionDate <= @ToDate
                   AND Quantity < 0
                   AND NetRate IS NOT NULL
                   AND LocationId = @LocationId
             ORDER BY TransactionDate DESC),
        0) AS LastSalePrice,

        ISNULL
        (
            (SELECT TOP 1 NetRate *
                (
                    ISNULL
                      (
                         (SELECT SUM (Quantity)
                          FROM [ProductStock]
                          WHERE     ProductId = s.ProductId
                                AND TransactionDate <= @ToDate
                                AND LocationId = @LocationId),
                         0)
                )
             FROM [ProductStock]
             WHERE     ProductId = s.ProductId
                   AND TransactionDate >= @FromDate
                   AND TransactionDate <= @ToDate
                   AND Quantity < 0
                   AND NetRate IS NOT NULL
                   AND LocationId = @LocationId
             ORDER BY TransactionDate DESC),
        0) AS LastSaleValue

    FROM
        [ProductStock] s

    LEFT JOIN
        dbo.Product p ON p.Id = s.ProductId
    LEFT JOIN
        dbo.ProductCategory pc ON pc.Id = p.ProductCategoryId

    GROUP BY s.ProductId,
        p.[Name],
        p.Code,
        p.ProductCategoryId,
        pc.Name,
        p.Rate;
END