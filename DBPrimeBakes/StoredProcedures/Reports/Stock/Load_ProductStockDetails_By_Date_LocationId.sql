CREATE PROCEDURE [dbo].[Load_ProductStockDetails_By_Date_LocationId]
	@FromDate DATETIME,
	@ToDate DATETIME,
	@LocationId INT
AS
BEGIN
	SELECT *
	FROM [dbo].[ProductStockDetails]
	WHERE TransactionDate >= @FromDate
	  AND TransactionDate <= @ToDate
	  AND LocationId = @LocationId
END