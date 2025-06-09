CREATE PROCEDURE [dbo].[Load_StockDetails_By_Date_LocationId]
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
            FROM Stock
            WHERE     RawMaterialId = s.RawMaterialId
                  AND TransactionDate < @FromDate
                  AND LocationId = @LocationId),
           0) AS OpeningStock,
        ISNULL
        (
           (SELECT SUM (Quantity)
            FROM Stock
            WHERE     RawMaterialId = s.RawMaterialId
                  AND TransactionDate >= @FromDate
                  AND TransactionDate < @ToDate
                  AND Type = 'Purchase'
                  AND LocationId = @LocationId),
           0) AS PurchaseStock,
        ISNULL
        (
           (SELECT SUM (Quantity)
            FROM Stock
            WHERE     RawMaterialId = s.RawMaterialId
                  AND TransactionDate >= @FromDate
                  AND TransactionDate < @ToDate
                  AND Type = 'Sale'
                  AND LocationId = @LocationId),
           0) AS SaleStock,
        ISNULL
        (
           (SELECT SUM (Quantity)
            FROM Stock
            WHERE     RawMaterialId = s.RawMaterialId
                  AND TransactionDate >= @FromDate
                  AND TransactionDate < @ToDate
                  AND LocationId = @LocationId),
           0) AS MonthlyStock,
        (  ISNULL
           (
              (SELECT SUM (Quantity)
               FROM Stock
               WHERE     RawMaterialId = s.RawMaterialId
                     AND TransactionDate < @FromDate
                     AND LocationId = @LocationId),
              0)
         + ISNULL
           (
              (SELECT SUM (Quantity)
               FROM Stock
               WHERE     RawMaterialId = s.RawMaterialId
                     AND TransactionDate >= @FromDate
                     AND TransactionDate < @ToDate
                     AND LocationId = @LocationId),
              0)) AS ClosingStock
    FROM
        Stock s

    LEFT JOIN
        dbo.RawMaterial r ON r.Id = s.RawMaterialId
    LEFT JOIN
        dbo.RawMaterialCategory rc ON rc.Id = r.RawMaterialCategoryId

    GROUP BY s.RawMaterialId,
            r.[Name],
            r.Code,
            r.RawMaterialCategoryId,
            rc.Name;
END