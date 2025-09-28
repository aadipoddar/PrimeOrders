CREATE PROCEDURE [dbo].[Insert_ProductStock]
	@Id INT,
	@ProductId INT, 
	@Quantity DECIMAL(7, 3), 
	@NetRate MONEY,
	@Type VARCHAR(20), 
	@TransactionNo VARCHAR(20),
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
			[NetRate],
			[Type], 
			[TransactionNo],
			[TransactionDate], 
			[LocationId]
		)
		VALUES
		(
			@ProductId, 
			@Quantity,
			@NetRate,
			@Type, 
			@TransactionNo,
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
			[NetRate] = @NetRate,
			[Type] = @Type, 
			[TransactionNo] = @TransactionNo,
			[TransactionDate] = @TransactionDate, 
			[LocationId] = @LocationId
		WHERE [Id] = @Id;
	END
END;
