CREATE PROCEDURE [dbo].[Load_SaleReturn_By_Sale]
	@SaleId INT
AS
BEGIN
	SELECT 		*
	FROM SaleReturn
	WHERE SaleId = @SaleId
END