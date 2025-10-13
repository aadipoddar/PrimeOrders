CREATE PROCEDURE [dbo].[Load_LedgerOverview_By_AccountingId]
	@AccountingId INT
AS
BEGIN
	SELECT *
	FROM Ledger_Overview
	WHERE AccountingId = @AccountingId
END