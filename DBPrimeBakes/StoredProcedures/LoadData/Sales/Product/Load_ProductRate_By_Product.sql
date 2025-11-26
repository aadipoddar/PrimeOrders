CREATE PROCEDURE [dbo].[Load_ProductRate_By_Product]
	@ProductId INT
AS
BEGIN
	SELECT	* 
	FROM [ProductLocation_Overview]
	WHERE ProductId = @ProductId
END