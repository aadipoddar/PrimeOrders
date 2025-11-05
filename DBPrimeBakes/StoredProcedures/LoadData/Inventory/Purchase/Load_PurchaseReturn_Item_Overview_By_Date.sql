CREATE PROCEDURE [dbo].[Load_PurchaseReturn_Item_Overview_By_Date]
	@StartDate DATETIME,
	@EndDate DATETIME
AS
BEGIN
	SELECT *
	FROM PurchaseReturn_Item_Overview
	WHERE TransactionDateTime BETWEEN @StartDate AND @EndDate
END