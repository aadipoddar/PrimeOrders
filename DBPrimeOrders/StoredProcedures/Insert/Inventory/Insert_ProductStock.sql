CREATE PROCEDURE [dbo].[Insert_ProductStock]
	@Id INT,
	@ProductId INT, 
	@Quantity DECIMAL(7, 3), 
	@Type VARCHAR(20), 
	@BillId INT, 
	@TransactionDate DATE, 
	@LocationId INT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[ProductStock] 
		(
			[ProductId], 
			[Quantity], 
			[Type], 
			[BillId], 
			[TransactionDate], 
			[LocationId]
		)
		VALUES
		(
			@ProductId, 
			@Quantity, 
			@Type, 
			@BillId, 
			@TransactionDate, 
			@LocationId
		);
	END
	ELSE

	BEGIN
		UPDATE [dbo].[ProductStock]
		SET 
			[ProductId] = @ProductId, 
			[Quantity] = @Quantity, 
			[Type] = @Type, 
			[BillId] = @BillId, 
			[TransactionDate] = @TransactionDate, 
			[LocationId] = @LocationId
		WHERE [Id] = @Id;
	END
END;
