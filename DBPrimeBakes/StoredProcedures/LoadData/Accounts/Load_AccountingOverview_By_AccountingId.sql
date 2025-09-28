CREATE PROCEDURE [dbo].[Load_AccountingOverview_By_AccountingId]
	@AccountingId INT
AS
BEGIN
	SELECT *
	FROM Accounting_Overview
	WHERE AccountingId = @AccountingId
END