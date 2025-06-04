CREATE PROCEDURE [dbo].[Insert_RawMaterial]
	@Id INT,
	@Name VARCHAR(250),
	@Code VARCHAR(50),
	@RawMaterialCategoryId INT,
	@MRP MONEY,
	@TaxId INT,
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[RawMaterial] ([Name], [Code], [RawMaterialCategoryId], [MRP], [TaxId], [Status])
		VALUES (@Name, @Code, @RawMaterialCategoryId, @MRP, @TaxId, @Status);
	END
	ELSE
	BEGIN
		UPDATE [dbo].[RawMaterial]
		SET [Name] = @Name, 
			[Code] = @Code, 
			[RawMaterialCategoryId] = @RawMaterialCategoryId, 
			[MRP] = @MRP, 
			[TaxId] = @TaxId, 
			[Status] = @Status
		WHERE [Id] = @Id;
	END
END