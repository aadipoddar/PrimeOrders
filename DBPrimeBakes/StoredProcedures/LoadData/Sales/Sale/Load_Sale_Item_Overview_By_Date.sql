CREATE PROCEDURE [dbo].[Load_Sale_Item_Overview_By_Date]
	@StartDate DATETIME,
	@EndDate DATETIME
AS
BEGIN
	SELECT *
	FROM Sale_Item_Overview
	WHERE TransactionDateTime BETWEEN @StartDate AND @EndDate
END