CREATE PROCEDURE [dbo].[Load_FinancialYear_By_Date]
	@Date DATE
AS
BEGIN
	SELECT *
	FROM FinancialYear
	WHERE @Date BETWEEN StartDate AND EndDate
	AND Status = 1
END