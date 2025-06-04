CREATE PROCEDURE [dbo].[Load_RecipeDetail_By_Recipe]
	@RecipeId INT
AS
BEGIN
	SELECT
		*
	FROM RecipeDetail
	WHERE RecipeId = @RecipeId
		AND Status = 1
END