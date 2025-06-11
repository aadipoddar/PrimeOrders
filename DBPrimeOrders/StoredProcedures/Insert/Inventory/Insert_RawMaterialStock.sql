CREATE PROCEDURE [dbo].[Insert_RawMaterialStock]
	@Id INT,
	@RawMaterialId INT, 
	@Quantity DECIMAL(7, 3), 
	@Type VARCHAR(20), 
	@BillId INT, 
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
			[Type], 
			[BillId], 
			[TransactionDate], 
			[LocationId]
		)
		VALUES
		(
			@RawMaterialId, 
			@Quantity, 
			@Type, 
			@BillId, 
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
			[Type] = @Type, 
			[BillId] = @BillId, 
			[TransactionDate] = @TransactionDate, 
			[LocationId] = @LocationId
		WHERE [Id] = @Id;
	END
END;
