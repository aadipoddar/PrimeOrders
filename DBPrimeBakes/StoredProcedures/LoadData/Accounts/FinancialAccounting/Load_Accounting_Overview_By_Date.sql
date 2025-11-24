CREATE PROCEDURE [dbo].[Load_Accounting_Overview_By_Date]
	@StartDate DATETIME,
	@EndDate DATETIME,
	@OnlyActive BIT = 1
AS
BEGIN
	SELECT *
	FROM Accounting_Overview
	WHERE TransactionDateTime BETWEEN @StartDate AND @EndDate
	  AND (@OnlyActive = 0 OR Status = 1)
END