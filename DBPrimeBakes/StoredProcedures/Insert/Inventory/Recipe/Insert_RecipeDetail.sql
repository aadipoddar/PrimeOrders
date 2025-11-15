CREATE PROCEDURE [dbo].[Insert_RecipeDetail]
	@Id INT OUTPUT,
	@RecipeId INT,
	@RawMaterialId INT,
	@Quantity MONEY,
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[RecipeDetail] ([RecipeId], [RawMaterialId], [Quantity], [Status])
		VALUES (@RecipeId, @RawMaterialId, @Quantity, @Status);

		SET @Id = SCOPE_IDENTITY();
	END
	ELSE
	BEGIN
		UPDATE [dbo].[RecipeDetail]
		SET [RecipeId] = @RecipeId, 
			[RawMaterialId] = @RawMaterialId, 
			[Quantity] = @Quantity, 
			[Status] = @Status
		WHERE [Id] = @Id;
	END

	SELECT @Id AS Id;
END