CREATE PROCEDURE [dbo].[Insert_OrderDetail]
	@Id INT,
	@OrderId INT, 
	@ProductId INT, 
	@Quantity DECIMAL(7, 3),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[OrderDetail]
		(
			OrderId, 
			ProductId, 
			Quantity, 
			Status
		) VALUES
		(
			@OrderId, 
			@ProductId, 
			@Quantity, 
			@Status
		);
	END
	ELSE
	BEGIN
		UPDATE [dbo].[OrderDetail]
		SET 
			OrderId = @OrderId, 
			ProductId = @ProductId, 
			Quantity = @Quantity, 
			Status = @Status
		WHERE Id = @Id;
	END
END;