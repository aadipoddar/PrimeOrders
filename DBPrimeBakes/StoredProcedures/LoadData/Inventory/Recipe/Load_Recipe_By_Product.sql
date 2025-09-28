CREATE PROCEDURE [dbo].[Load_Recipe_By_Product]
	@ProductId INT
AS
BEGIN
	SELECT
	*
	FROM Recipe
	WHERE ProductId = @ProductId
		AND Status = 1
END