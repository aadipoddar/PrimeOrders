CREATE PROCEDURE [dbo].[Insert_Product]
	@Id INT OUTPUT,
	@Name VARCHAR(500),
	@Code VARCHAR(10),
	@ProductCategoryId INT,
	@Rate MONEY,
	@TaxId INT,
	@Remarks VARCHAR(MAX),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Product] ([ProductCategoryId], [Code], [Name], [TaxId], [Rate], [Status], [Remarks])
		VALUES (@ProductCategoryId, @Code, @Name, @TaxId, @Rate, @Status, @Remarks);
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
			[Remarks] = @Remarks,
			[Status] = @Status
		WHERE [Id] = @Id;
	END
	SELECT @Id AS Id;
END