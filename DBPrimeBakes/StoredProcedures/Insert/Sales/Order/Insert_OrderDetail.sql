CREATE PROCEDURE [dbo].[Insert_OrderDetail]
	@Id INT OUTPUT,
	@OrderId INT, 
	@ProductId INT, 
	@Quantity MONEY,
	@Remarks VARCHAR(MAX),
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
			Remarks,
			Status
		) VALUES
		(
			@OrderId, 
			@ProductId, 
			@Quantity, 
			@Remarks,
			@Status
		);
		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[OrderDetail]
		SET 
			OrderId = @OrderId, 
			ProductId = @ProductId, 
			Quantity = @Quantity, 
			Remarks = @Remarks,
			Status = @Status
		WHERE Id = @Id;
	END

	SELECT @Id AS Id;
END;