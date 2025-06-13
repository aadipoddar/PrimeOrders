CREATE PROCEDURE [dbo].[Load_Order_By_Sale]
	@SaleId INT
AS
BEGIN
	SELECT *
	FROM [dbo].[Order] 
	WHERE SaleId = @SaleId
		AND Status = 1
END