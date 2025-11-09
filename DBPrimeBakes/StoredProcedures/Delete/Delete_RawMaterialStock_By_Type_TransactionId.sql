CREATE PROCEDURE [dbo].[Delete_RawMaterialStock_By_Type_TransactionId]
	@Type VARCHAR(20),
	@TransactionId INT
AS
BEGIN
	DELETE FROM [dbo].[RawMaterialStock]
	WHERE [Type] = @Type
	  AND [TransactionId] = @TransactionId
END