CREATE PROCEDURE [dbo].[Load_PurchaseOverview_By_Date]
	@StartDate DATETIME,
	@EndDate DATETIME
AS
BEGIN
	SELECT *
	FROM Purchase_Overview
	WHERE TransactionDateTime BETWEEN @StartDate AND @EndDate
END