CREATE PROCEDURE [dbo].[Load_PurchaseOverview_By_Date]
	@StartDate DATETIME,
	@EndDate DATETIME,
	@OnlyActive BIT = 1
AS
BEGIN
	SELECT *
	FROM Purchase_Overview
	WHERE TransactionDateTime BETWEEN @StartDate AND @EndDate
	  AND (@OnlyActive = 0 OR Status = 1)
END