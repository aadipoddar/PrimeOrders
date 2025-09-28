CREATE PROCEDURE [dbo].[Delete_RawMaterialStock_By_TransactionNo]
	@TransactionNo VARCHAR(20)
AS
BEGIN
	DELETE FROM [dbo].[RawMaterialStock]
	WHERE [TransactionNo] = @TransactionNo;
END