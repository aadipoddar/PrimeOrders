CREATE PROCEDURE [dbo].[Load_Product_By_ProductCategory]
	@ProductCategoryId INT
AS
BEGIN
	SELECT *
	FROM dbo.Product
	WHERE ProductCategoryId = @ProductCategoryId;
END