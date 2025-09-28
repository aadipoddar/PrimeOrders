CREATE PROCEDURE [dbo].[Load_SaleReturnOverview_By_SaleReturnId]
	@SaleReturnId INT
AS
BEGIN
	SELECT *
	FROM SaleReturn_Overview
	WHERE SaleReturnId = @SaleReturnId
END