CREATE PROCEDURE [dbo].[Insert_RawMaterial]
	@Id INT,
	@Name VARCHAR(250),
	@Code VARCHAR(50),
	@RawMaterialCategoryId INT,
	@Rate MONEY,
	@UnitOfMeasurement VARCHAR(20),
	@TaxId INT,
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[RawMaterial] ([Name], [Code], [RawMaterialCategoryId], [Rate], [UnitOfMeasurement], [TaxId], [Status])
		VALUES (@Name, @Code, @RawMaterialCategoryId, @Rate, @UnitOfMeasurement, @TaxId, @Status);
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
			[Status] = @Status
		WHERE [Id] = @Id;
	END
END