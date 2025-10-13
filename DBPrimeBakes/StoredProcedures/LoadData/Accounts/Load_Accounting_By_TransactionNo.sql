CREATE PROCEDURE [dbo].[Load_Accounting_By_TransactionNo]
	@TransactionNo VARCHAR(20)
AS
BEGIN
	SELECT *
	FROM Accounting
	WHERE [TransactionNo] = @TransactionNo
	AND Status = 1
END