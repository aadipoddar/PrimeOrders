CREATE PROCEDURE [dbo].[Load_RawMaterialStockDetails_By_Date_LocationId]
	@FromDate DATETIME,
	@ToDate DATETIME,
	@LocationId INT
AS
BEGIN
	SELECT *
	FROM [dbo].[RawMaterialStockDetails]
	WHERE TransactionDate >= @FromDate
	  AND TransactionDate <= @ToDate
	  AND LocationId = @LocationId
END