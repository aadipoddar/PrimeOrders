CREATE PROCEDURE [dbo].[Load_LedgerDetails_By_Date_Ledger]
	@FromDate DATETIME,
	@ToDate DATETIME,
	@LedgerId INT
AS
BEGIN
	IF @LedgerId = 0
	BEGIN
		SELECT *
		FROM dbo.Ledger_Overview
		WHERE AccountingDate BETWEEN @FromDate AND @ToDate;
	END
	ELSE
	BEGIN
		SELECT *
		FROM dbo.Ledger_Overview v
		WHERE AccountingDate BETWEEN @FromDate AND @ToDate
		  AND v.LedgerId = @LedgerId;
	END
END