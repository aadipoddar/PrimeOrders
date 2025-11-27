CREATE PROCEDURE [dbo].[Insert_KitchenProductionDetail]
	@Id INT OUTPUT,
	@MasterId INT,
	@ProductId INT,
	@Quantity MONEY,
	@Rate MONEY,
	@Total MONEY,
	@Remarks VARCHAR(MAX),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[KitchenProductionDetail]
		(
			MasterId,
			ProductId,
			Quantity,
			Rate,
			Total,
			Remarks,
			Status
		)
		VALUES
		(
			@MasterId,
			@ProductId,
			@Quantity,
			@Rate,
			@Total,
			@Remarks,
			@Status
		);

		SET @Id = SCOPE_IDENTITY();
	END
	ELSE
	BEGIN
		UPDATE [dbo].[KitchenProductionDetail]
		SET [MasterId] = @MasterId,
			ProductId = @ProductId,
			Quantity = @Quantity,
			Rate = @Rate,
			Total = @Total,
			Remarks = @Remarks,
			Status = @Status
		WHERE Id = @Id;
	END

	SELECT @Id AS Id;
END