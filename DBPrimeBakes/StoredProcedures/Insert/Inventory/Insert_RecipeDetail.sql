CREATE PROCEDURE [dbo].[Insert_RecipeDetail]
	@Id INT,
	@RecipeId INT,
	@RawMaterialId INT,
	@Quantity DECIMAL(7, 3),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[RecipeDetail] ([RecipeId], [RawMaterialId], [Quantity], [Status])
		VALUES (@RecipeId, @RawMaterialId, @Quantity, @Status);
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
END