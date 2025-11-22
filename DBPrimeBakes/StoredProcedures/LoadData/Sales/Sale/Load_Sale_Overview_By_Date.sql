CREATE PROCEDURE [dbo].[Load_Sale_Overview_By_Date]
	@StartDate DATETIME,
	@EndDate DATETIME,
	@OnlyActive BIT = 1
AS
BEGIN
	SELECT *
	FROM Sale_Overview
	WHERE TransactionDateTime BETWEEN @StartDate AND @EndDate
	  AND (@OnlyActive = 0 OR Status = 1)
END