CREATE PROCEDURE [dbo].[Load_SaleDetail_By_Sale]
	@SaleId INT
AS
BEGIN
	SELECT
		*
	FROM SaleDetail
	WHERE SaleId = @SaleId
		AND Status = 1
END