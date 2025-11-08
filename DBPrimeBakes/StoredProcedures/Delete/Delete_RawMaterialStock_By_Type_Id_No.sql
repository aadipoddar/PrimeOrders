CREATE PROCEDURE [dbo].[Delete_RawMaterialStock_By_Type_Id_No]
	@Type VARCHAR(20),
	@TransactionId INT = NULL,
	@TransactionNo VARCHAR(20)
AS
BEGIN
	IF @TransactionId IS NULL
	BEGIN
		DELETE FROM [dbo].[RawMaterialStock]
		WHERE [Type] = @Type
		  AND [TransactionNo] = @TransactionNo
	END

	ELSE
	BEGIN
		DELETE FROM [dbo].[RawMaterialStock]
		WHERE [Type] = @Type
		  AND [TransactionId] = @TransactionId
		  AND [TransactionNo] = @TransactionNo
	END
END