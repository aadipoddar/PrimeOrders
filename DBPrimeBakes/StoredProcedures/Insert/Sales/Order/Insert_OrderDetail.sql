CREATE PROCEDURE [dbo].[Insert_OrderDetail]
	@Id INT OUTPUT,
	@MasterId INT, 
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
			[MasterId], 
			ProductId, 
			Quantity, 
			Remarks,
			Status
		) VALUES
		(
			@MasterId, 
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
			[MasterId] = @MasterId, 
			ProductId = @ProductId, 
			Quantity = @Quantity, 
			Remarks = @Remarks,
			Status = @Status
		WHERE Id = @Id;
	END

	SELECT @Id AS Id;
END;