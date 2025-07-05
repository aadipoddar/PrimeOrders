CREATE PROCEDURE [dbo].[Load_SaleReturnDetail_By_SaleReturn]
	@SaleReturnId INT
AS
BEGIN
	SELECT
		*
	FROM SaleReturnDetail
	WHERE SaleReturnId = @SaleReturnId
		AND Status = 1
END