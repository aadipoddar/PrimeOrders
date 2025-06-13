CREATE PROCEDURE [dbo].[Load_KitchenProductionDetails_By_Date]
	@FromDate DATETIME,
	@ToDate DATETIME
AS
BEGIN
	SELECT *
	FROM dbo.KitchenProduction_Overview v
	WHERE ProductionDate BETWEEN @FromDate AND @ToDate;
END