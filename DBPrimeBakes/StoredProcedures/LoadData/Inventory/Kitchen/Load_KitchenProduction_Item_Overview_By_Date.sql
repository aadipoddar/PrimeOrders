CREATE PROCEDURE [dbo].[Load_KitchenProduction_Item_Overview_By_Date]
	@StartDate DATETIME,
	@EndDate DATETIME
AS
BEGIN
	SELECT *
	FROM KitchenProduction_Item_Overview
	WHERE TransactionDateTime BETWEEN @StartDate AND @EndDate
END
