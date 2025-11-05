CREATE PROCEDURE [dbo].[Load_FinancialYear_By_DateTime]
	@TransactionDateTime DateTime
AS
BEGIN
	SELECT *
	FROM FinancialYear
	WHERE @TransactionDateTime BETWEEN StartDate AND EndDate
	AND Status = 1
END