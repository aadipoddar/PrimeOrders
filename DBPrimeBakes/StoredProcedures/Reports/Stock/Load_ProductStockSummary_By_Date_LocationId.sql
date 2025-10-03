﻿CREATE PROCEDURE [dbo].[Load_ProductStockSummary_By_Date_LocationId]
	@FromDate DATETIME,
	@ToDate DATETIME,
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
                  AND TransactionDate < @ToDate
                  AND Type = 'Purchase'
                  AND LocationId = @LocationId),
           0) AS PurchaseStock,

        ISNULL
        (
           (SELECT SUM (Quantity)
            FROM [ProductStock]
            WHERE     ProductId = s.ProductId
                  AND TransactionDate >= @FromDate
                  AND TransactionDate < @ToDate
                  AND Type = 'Sale'
                  AND LocationId = @LocationId),
           0) AS SaleStock,

        ISNULL
        (
           (SELECT SUM (Quantity)
            FROM [ProductStock]
            WHERE     ProductId = s.ProductId
                  AND TransactionDate >= @FromDate
                  AND TransactionDate < @ToDate
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
            (SELECT AVG (NetRate)
             FROM [ProductStock]
             WHERE     ProductId = s.ProductId
                   AND TransactionDate >= @FromDate
                   AND TransactionDate <= @ToDate
                   AND Type = 'Sale'
                   AND NetRate IS NOT NULL
                   AND LocationId = @LocationId),
        0) AS AveragePrice,

        ISNULL
        (
            (SELECT TOP 1 NetRate
             FROM [ProductStock]
             WHERE     ProductId = s.ProductId
                   AND TransactionDate >= @FromDate
                   AND TransactionDate <= @ToDate
                   AND Type = 'Sale'
                   AND NetRate IS NOT NULL
                   AND LocationId = @LocationId
             ORDER BY TransactionDate DESC),
        0) AS LastSalePrice,

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
                   AND Type = 'Sale'
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
                   AND Type = 'Sale'
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
            pc.Name;
END