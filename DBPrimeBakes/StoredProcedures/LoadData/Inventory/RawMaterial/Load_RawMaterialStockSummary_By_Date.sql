CREATE PROCEDURE [dbo].[Load_RawMaterialStockSummary_By_Date]
	@FromDate DATETIME,
	@ToDate DATETIME
AS
BEGIN
	SELECT
        s.RawMaterialId,
        r.[Name] RawMaterialName,
        r.Code RawMaterialCode,
        r.RawMaterialCategoryId,
        rc.[Name] RawMaterialCategoryName,
        r.UnitOfMeasurement,

        ISNULL
        (
           (SELECT SUM (Quantity)
            FROM [RawMaterialStock]
            WHERE     RawMaterialId = s.RawMaterialId
                  AND TransactionDate < @FromDate),
           0) AS OpeningStock,

        ISNULL
        (
           (SELECT SUM (Quantity)
            FROM [RawMaterialStock]
            WHERE     RawMaterialId = s.RawMaterialId
                  AND TransactionDate >= @FromDate
                  AND TransactionDate <= @ToDate
                  AND Quantity > 0),
           0) AS PurchaseStock,

        ISNULL
        (
           (SELECT SUM (Quantity)
            FROM [RawMaterialStock]
            WHERE     RawMaterialId = s.RawMaterialId
                  AND TransactionDate >= @FromDate
                  AND TransactionDate <= @ToDate
                  AND Quantity < 0),
           0) AS SaleStock,

        ISNULL
        (
           (SELECT SUM (Quantity)
            FROM [RawMaterialStock]
            WHERE     RawMaterialId = s.RawMaterialId
                  AND TransactionDate >= @FromDate
                  AND TransactionDate <= @ToDate),
           0) AS MonthlyStock,

        ISNULL
        (
           (SELECT SUM (Quantity)
            FROM [RawMaterialStock]
            WHERE     RawMaterialId = s.RawMaterialId
                  AND TransactionDate <= @ToDate),
           0) AS ClosingStock,

        [r].Rate,

        ISNULL
        (
            (SELECT [r].Rate *
                (
                    ISNULL
                     (
                        (SELECT SUM (Quantity)
                         FROM [RawMaterialStock]
                         WHERE     RawMaterialId = s.RawMaterialId
                               AND TransactionDate <= @ToDate),
                        0)
                )
             ), 0) AS ClosingValue,

        ISNULL
        (
            (SELECT AVG (NetRate)
             FROM [RawMaterialStock]
             WHERE     RawMaterialId = s.RawMaterialId
                   AND TransactionDate >= @FromDate
                   AND TransactionDate <= @ToDate
                   AND Quantity > 0
                   AND NetRate IS NOT NULL),
        0) AS AveragePrice,

        ISNULL
        (
            (SELECT AVG (NetRate) * 
                (
                     ISNULL
                     (
                        (SELECT SUM (Quantity)
                         FROM [RawMaterialStock]
                         WHERE     RawMaterialId = s.RawMaterialId
                               AND TransactionDate <= @ToDate),
                        0)
                )
             FROM [RawMaterialStock]
             WHERE     RawMaterialId = s.RawMaterialId
                   AND TransactionDate >= @FromDate
                   AND TransactionDate <= @ToDate
                   AND Quantity > 0
                   AND NetRate IS NOT NULL),
        0) AS WeightedAverageValue,

        ISNULL
        (
            (SELECT TOP 1 NetRate
             FROM [RawMaterialStock]
             WHERE     RawMaterialId = s.RawMaterialId
                   AND TransactionDate >= @FromDate
                   AND TransactionDate <= @ToDate
                   AND Quantity > 0
                   AND NetRate IS NOT NULL
             ORDER BY TransactionDate DESC),
        0) AS LastPurchasePrice,

        ISNULL
        (
            (SELECT TOP 1 NetRate *
                (
                    ISNULL
                     (
                        (SELECT SUM (Quantity)
                         FROM [RawMaterialStock]
                         WHERE     RawMaterialId = s.RawMaterialId
                               AND TransactionDate <= @ToDate),
                        0)
                )
             FROM [RawMaterialStock]
             WHERE     RawMaterialId = s.RawMaterialId
                   AND TransactionDate >= @FromDate
                   AND TransactionDate <= @ToDate
                   AND Quantity > 0
                   AND NetRate IS NOT NULL
             ORDER BY TransactionDate DESC),
        0) AS LastPurchaseValue
    FROM
        [RawMaterialStock] s

    LEFT JOIN
        dbo.RawMaterial r ON r.Id = s.RawMaterialId
    LEFT JOIN
        dbo.RawMaterialCategory rc ON rc.Id = r.RawMaterialCategoryId
    
    GROUP BY s.RawMaterialId,
            r.[Name],
            r.Code,
            r.RawMaterialCategoryId,
            rc.Name,
            r.UnitOfMeasurement,
            r.Rate;
END