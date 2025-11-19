CREATE PROCEDURE [dbo].[Load_SaleReturn_Item_Overview_By_Date]
	@StartDate DATETIME,
	@EndDate DATETIME
AS
BEGIN
	SELECT *
	FROM SaleReturn_Item_Overview
	WHERE TransactionDateTime BETWEEN @StartDate AND @EndDate
END