CREATE PROCEDURE [dbo].[Insert_KitchenProductionDetail]
	@Id INT,
	@KitchenProductionId INT,
	@ProductId INT,
	@Quantity DECIMAL(7, 3),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[KitchenProductionDetail] (KitchenProductionId, ProductId, Quantity, Status)
		VALUES (@KitchenProductionId, @ProductId, @Quantity, @Status);
	END
	ELSE
	BEGIN
		UPDATE [dbo].[KitchenProductionDetail]
		SET KitchenProductionId = @KitchenProductionId,
			ProductId = @ProductId,
			Quantity = @Quantity,
			Status = @Status
		WHERE Id = @Id;
	END
END