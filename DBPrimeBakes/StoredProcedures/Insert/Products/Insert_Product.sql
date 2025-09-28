CREATE PROCEDURE [dbo].[Insert_Product]
	@Id INT,
	@Code VARCHAR(50),
	@Name VARCHAR(250),
	@ProductCategoryId INT,
	@Rate MONEY,
	@TaxId INT,
	@LocationId INT,
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Product] ([ProductCategoryId], [Code], [Name], [TaxId], [Rate], [Status], [LocationId])
		VALUES (@ProductCategoryId, @Code, @Name, @TaxId, @Rate, @Status, @LocationId);
	END

	ELSE
	BEGIN
		UPDATE [dbo].[Product]
		SET [ProductCategoryId] = @ProductCategoryId, 
			[Code] = @Code, 
			[Name] = @Name, 
			[TaxId] = @TaxId, 
			[Rate] = @Rate, 
			[LocationId] = @LocationId,
			[Status] = @Status
		WHERE [Id] = @Id;
	END
END