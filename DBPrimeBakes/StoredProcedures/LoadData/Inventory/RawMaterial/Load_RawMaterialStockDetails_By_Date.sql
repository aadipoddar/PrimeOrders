CREATE PROCEDURE [dbo].[Load_RawMaterialStockDetails_By_Date]
	@FromDate DATETIME,
	@ToDate DATETIME
AS
BEGIN
	SELECT *
	FROM [dbo].[RawMaterialStockDetails]
	WHERE TransactionDate >= @FromDate
	  AND TransactionDate <= @ToDate
END