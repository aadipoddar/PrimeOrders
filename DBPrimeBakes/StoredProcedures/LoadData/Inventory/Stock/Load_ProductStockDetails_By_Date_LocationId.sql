CREATE PROCEDURE [dbo].[Load_ProductStockDetails_By_Date_LocationId]
	@FromDate DATE,
	@ToDate DATE,
	@LocationId INT
AS
BEGIN
	SELECT *
	FROM [dbo].[ProductStockDetails]
	WHERE TransactionDate >= @FromDate
	  AND TransactionDate <= @ToDate
	  AND LocationId = @LocationId
END