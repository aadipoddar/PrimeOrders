CREATE PROCEDURE [dbo].[Delete_ProductStock_By_TransactionNo]
	@TransactionNo VARCHAR(20)
AS
BEGIN
	DELETE FROM [dbo].[ProductStock]
	WHERE [TransactionNo] = @TransactionNo;
END