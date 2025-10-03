CREATE PROCEDURE [dbo].[Load_RawMaterialStockSummary_By_Date_LocationId]
	@FromDate DATETIME,
	@ToDate DATETIME,
	@LocationId INT
AS
BEGIN
	SELECT
        s.RawMaterialId,
        r.[Name] RawMaterialName,
        r.Code RawMaterialCode,
        r.RawMaterialCategoryId,
        rc.[Name] RawMaterialCategoryName,

        ISNULL
        (
           (SELECT SUM (Quantity)
            FROM [RawMaterialStock]
            WHERE     RawMaterialId = s.RawMaterialId
                  AND TransactionDate < @FromDate
                  AND LocationId = @LocationId),
           0) AS OpeningStock,

        ISNULL
        (
           (SELECT SUM (Quantity)
            FROM [RawMaterialStock]
            WHERE     RawMaterialId = s.RawMaterialId
                  AND TransactionDate >= @FromDate
                  AND TransactionDate <= @ToDate
                  AND Type = 'Purchase'
                  AND LocationId = @LocationId),
           0) AS PurchaseStock,

        ISNULL
        (
           (SELECT SUM (Quantity)
            FROM [RawMaterialStock]
            WHERE     RawMaterialId = s.RawMaterialId
                  AND TransactionDate >= @FromDate
                  AND TransactionDate <= @ToDate
                  AND Type = 'KitchenIssue'
                  AND LocationId = @LocationId),
           0) AS SaleStock,

        ISNULL
        (
           (SELECT SUM (Quantity)
            FROM [RawMaterialStock]
            WHERE     RawMaterialId = s.RawMaterialId
                  AND TransactionDate >= @FromDate
                  AND TransactionDate <= @ToDate
                  AND LocationId = @LocationId),
           0) AS MonthlyStock,

        ISNULL
        (
           (SELECT SUM (Quantity)
            FROM [RawMaterialStock]
            WHERE     RawMaterialId = s.RawMaterialId
                  AND TransactionDate <= @ToDate
                  AND LocationId = @LocationId),
           0) AS ClosingStock,

        ISNULL
        (
            (SELECT AVG (NetRate)
             FROM [RawMaterialStock]
             WHERE     RawMaterialId = s.RawMaterialId
                   AND TransactionDate >= @FromDate
                   AND TransactionDate <= @ToDate
                   AND Type = 'Purchase'
                   AND NetRate IS NOT NULL
                   AND LocationId = @LocationId),
        0) AS AveragePrice,

        ISNULL
        (
            (SELECT TOP 1 NetRate
             FROM [RawMaterialStock]
             WHERE     RawMaterialId = s.RawMaterialId
                   AND TransactionDate >= @FromDate
                   AND TransactionDate <= @ToDate
                   AND Type = 'Purchase'
                   AND NetRate IS NOT NULL
                   AND LocationId = @LocationId
             ORDER BY TransactionDate DESC),
        0) AS LastPurchasePrice,

        ISNULL
        (
            (SELECT AVG (NetRate) * 
                (
                     ISNULL
                     (
                        (SELECT SUM (Quantity)
                         FROM [RawMaterialStock]
                         WHERE     RawMaterialId = s.RawMaterialId
                               AND TransactionDate <= @ToDate
                               AND LocationId = @LocationId),
                        0)
                )
             FROM [RawMaterialStock]
             WHERE     RawMaterialId = s.RawMaterialId
                   AND TransactionDate >= @FromDate
                   AND TransactionDate <= @ToDate
                   AND Type = 'Purchase'
                   AND NetRate IS NOT NULL
                   AND LocationId = @LocationId),
        0) AS WeightedAverageValue,

        ISNULL
        (
            (SELECT TOP 1 NetRate *
                (
                    ISNULL
                     (
                        (SELECT SUM (Quantity)
                         FROM [RawMaterialStock]
                         WHERE     RawMaterialId = s.RawMaterialId
                               AND TransactionDate <= @ToDate
                               AND LocationId = @LocationId),
                        0)
                )
             FROM [RawMaterialStock]
             WHERE     RawMaterialId = s.RawMaterialId
                   AND TransactionDate >= @FromDate
                   AND TransactionDate <= @ToDate
                   AND Type = 'Purchase'
                   AND NetRate IS NOT NULL
                   AND LocationId = @LocationId
             ORDER BY TransactionDate DESC),
        0) AS LastPurchaseValue
    FROM
        [RawMaterialStock] s

    LEFT JOIN
        dbo.RawMaterial r ON r.Id = s.RawMaterialId
    LEFT JOIN
        dbo.RawMaterialCategory rc ON rc.Id = r.RawMaterialCategoryId
    
    WHERE s.LocationId = @LocationId

    GROUP BY s.RawMaterialId,
            r.[Name],
            r.Code,
            r.RawMaterialCategoryId,
            rc.Name;
END