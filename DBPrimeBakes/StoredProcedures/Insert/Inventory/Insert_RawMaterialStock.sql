CREATE PROCEDURE [dbo].[Insert_RawMaterialStock]
	@Id INT OUTPUT,
	@RawMaterialId INT, 
	@Quantity MONEY, 
	@NetRate MONEY,
	@Type VARCHAR(20), 
	@TransactionId INT,
	@TransactionNo VARCHAR(20),
	@TransactionDate DATE
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[RawMaterialStock] 
		(
			[RawMaterialId], 
			[Quantity], 
			[NetRate],
			[Type], 
			[TransactionId],
			[TransactionNo],
			[TransactionDate]
		)
		VALUES
		(
			@RawMaterialId, 
			@Quantity, 
			@NetRate,
			@Type, 
			@TransactionId,
			@TransactionNo,
			@TransactionDate
		);

		SET @Id = SCOPE_IDENTITY();
	END
	ELSE

	BEGIN
		UPDATE [dbo].[RawMaterialStock]
		SET 
			[RawMaterialId] = @RawMaterialId, 
			[Quantity] = @Quantity, 
			[NetRate] = @NetRate,
			[Type] = @Type, 
			[TransactionId] = @TransactionId,
			[TransactionNo] = @TransactionNo,
			[TransactionDate] = @TransactionDate
		WHERE [Id] = @Id;
	END

	SELECT @Id AS Id;
END;
