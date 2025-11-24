CREATE PROCEDURE [dbo].[Load_AccountingDetail_By_Accounting]
	@AccountingId INT
AS
BEGIN
	SELECT *
	FROM [dbo].[AccountingDetail]
	WHERE [AccountingId] = @AccountingId
	AND Status = 1
END