CREATE PROCEDURE [dbo].[Delete_ProductStock_By_Type_TransactionId]
	@Type VARCHAR(20),
	@TransactionId INT
AS
BEGIN
	DELETE FROM [dbo].[ProductStock]
	WHERE [Type] = @Type
	  AND [TransactionId] = @TransactionId
END