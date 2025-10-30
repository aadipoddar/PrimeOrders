CREATE PROCEDURE [dbo].[Insert_RawMaterialStock]
	@Id INT,
	@RawMaterialId INT, 
	@Quantity MONEY, 
	@NetRate MONEY,
	@Type VARCHAR(20), 
	@TransactionNo VARCHAR(20),
	@TransactionDate DATE, 
	@LocationId INT
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
			[TransactionNo],
			[TransactionDate], 
			[LocationId]
		)
		VALUES
		(
			@RawMaterialId, 
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
		UPDATE [dbo].[RawMaterialStock]
		SET 
			[RawMaterialId] = @RawMaterialId, 
			[Quantity] = @Quantity, 
			[NetRate] = @NetRate,
			[Type] = @Type, 
			[TransactionNo] = @TransactionNo,
			[TransactionDate] = @TransactionDate, 
			[LocationId] = @LocationId
		WHERE [Id] = @Id;
	END
END;
