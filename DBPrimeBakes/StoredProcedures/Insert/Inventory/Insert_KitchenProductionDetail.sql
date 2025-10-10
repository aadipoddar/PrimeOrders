CREATE PROCEDURE [dbo].[Insert_KitchenProductionDetail]
	@Id INT,
	@KitchenProductionId INT,
	@ProductId INT,
	@Quantity DECIMAL(7, 3),
	@Rate MONEY,
	@Total MONEY,
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[KitchenProductionDetail] (KitchenProductionId, ProductId, Quantity, Rate, Total, Status)
		VALUES (@KitchenProductionId, @ProductId, @Quantity, @Rate, @Total, @Status);
	END
	ELSE
	BEGIN
		UPDATE [dbo].[KitchenProductionDetail]
		SET KitchenProductionId = @KitchenProductionId,
			ProductId = @ProductId,
			Quantity = @Quantity,
			Rate = @Rate,
			Total = @Total,
			Status = @Status
		WHERE Id = @Id;
	END
END