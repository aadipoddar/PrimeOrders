CREATE PROCEDURE [dbo].[Delete_ProductStock_By_Type_TransactionId_LocationId]
	@Type VARCHAR(20),
	@TransactionId INT,
	@LocationId INT
AS
BEGIN
	DELETE FROM [dbo].[ProductStock]
	WHERE [Type] = @Type
	  AND [TransactionId] = @TransactionId
	  AND [LocationId] = @LocationId
END