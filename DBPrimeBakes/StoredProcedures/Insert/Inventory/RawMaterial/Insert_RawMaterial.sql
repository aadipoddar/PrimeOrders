CREATE PROCEDURE [dbo].[Insert_RawMaterial]
	@Id INT OUTPUT,
	@Name VARCHAR(500),
	@Code VARCHAR(10),
	@RawMaterialCategoryId INT,
	@Rate MONEY,
	@UnitOfMeasurement VARCHAR(20),
	@TaxId INT,
	@Remarks VARCHAR(MAX),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[RawMaterial] ([Name], [Code], [RawMaterialCategoryId], [Rate], [UnitOfMeasurement], [TaxId], [Remarks], [Status])
		VALUES (@Name, @Code, @RawMaterialCategoryId, @Rate, @UnitOfMeasurement, @TaxId, @Remarks, @Status);

		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[RawMaterial]
		SET [Name] = @Name, 
			[Code] = @Code, 
			[RawMaterialCategoryId] = @RawMaterialCategoryId, 
			[Rate] = @Rate, 
			[UnitOfMeasurement] = @UnitOfMeasurement,
			[TaxId] = @TaxId, 
			[Remarks] = @Remarks,
			[Status] = @Status
		WHERE [Id] = @Id;
	END

	SELECT @Id AS Id;
END