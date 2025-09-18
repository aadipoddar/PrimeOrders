CREATE PROCEDURE [dbo].[Load_Product_By_LocationRate]
	@LocationId INT
AS
BEGIN
	SELECT
		[Id],
		[Code],
		[Name],
		[ProductCategoryId],
	
		ISNULL(
			(SELECT TOP 1 [Rate]
				FROM ProductRate
				WHERE ProductId = Product.Id
				AND LocationId = @LocationId
				AND Status = 1
				),
			[Rate]) AS [Rate],
	
		[TaxId],
		[LocationId],
		[Status]
	FROM Product
	WHERE Status = 1

END