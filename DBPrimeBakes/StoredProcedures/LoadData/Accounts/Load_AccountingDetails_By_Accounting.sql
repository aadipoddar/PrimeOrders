CREATE PROCEDURE [dbo].[Load_AccountingDetails_By_Accounting]
	@AccountingId INT
AS
BEGIN
	SELECT *
	FROM [dbo].[AccountingDetails]
	WHERE [AccountingId] = @AccountingId
	AND Status = 1
END