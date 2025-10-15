CREATE PROCEDURE [dbo].[Insert_Product]
	@Id INT OUTPUT,
	@Code VARCHAR(50),
	@Name VARCHAR(250),
	@ProductCategoryId INT,
	@Rate MONEY,
	@TaxId INT,
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Product] ([ProductCategoryId], [Code], [Name], [TaxId], [Rate], [Status])
		VALUES (@ProductCategoryId, @Code, @Name, @TaxId, @Rate, @Status);
		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[Product]
		SET [ProductCategoryId] = @ProductCategoryId, 
			[Code] = @Code, 
			[Name] = @Name, 
			[TaxId] = @TaxId, 
			[Rate] = @Rate, 
			[Status] = @Status
		WHERE [Id] = @Id;
	END
	SELECT @Id AS Id;
END