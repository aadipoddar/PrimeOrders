CREATE PROCEDURE [dbo].[Load_SaleOverview_By_SaleId]
	@SaleId INT
AS
BEGIN
	SELECT *
	FROM Sale_Overview
	WHERE SaleId = @SaleId
END