CREATE PROCEDURE [dbo].[Load_Accounting_Ledger_Overview_By_Date]
	@StartDate DATETIME,
	@EndDate DATETIME
AS
BEGIN
	SELECT *
	FROM Accounting_Ledger_Overview
	WHERE TransactionDateTime BETWEEN @StartDate AND @EndDate
END