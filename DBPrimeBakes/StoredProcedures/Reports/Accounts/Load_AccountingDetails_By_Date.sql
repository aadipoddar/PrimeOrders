CREATE PROCEDURE [dbo].[Load_AccountingDetails_By_Date]
	@FromDate DATE,
	@ToDate DATE
AS
BEGIN
	SELECT
		*
	FROM
		[dbo].[Accounting_Overview] AS ao
	WHERE
		ao.AccountingDate BETWEEN @FromDate AND @ToDate
END;