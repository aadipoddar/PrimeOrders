CREATE PROCEDURE [dbo].[Load_ProductRate_By_Product]
	@ProductId INT
AS
BEGIN
	SELECT	* 
	FROM ProductRate
	WHERE ProductId = @ProductId
	AND Status = 1
END