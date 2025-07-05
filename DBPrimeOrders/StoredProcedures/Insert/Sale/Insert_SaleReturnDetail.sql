CREATE PROCEDURE [dbo].[Insert_SaleReturnDetail]
	@Id INT,
	@SaleReturnId INT,
	@ProductId INT,
	@Quantity DECIMAL(5, 2),
	@Status BIT = 1
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[SaleReturnDetail] 
			([SaleReturnId], [ProductId], [Quantity], [Status])
		VALUES 
			(@SaleReturnId, @ProductId, @Quantity, @Status);
	END

	ELSE
	BEGIN
		UPDATE [dbo].[SaleReturnDetail]
		SET
			[SaleReturnId] = @SaleReturnId,
			[ProductId] = @ProductId,
			[Quantity] = @Quantity,
			[Status] = @Status
		WHERE Id = @Id;
	END
END