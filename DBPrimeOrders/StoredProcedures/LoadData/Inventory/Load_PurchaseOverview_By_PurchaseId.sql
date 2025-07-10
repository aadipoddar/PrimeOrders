CREATE PROCEDURE [dbo].[Load_PurchaseOverview_By_PurchaseId]
	@PurchaseId INT
AS
BEGIN
	SELECT *
	FROM Purchase_Overview
	WHERE PurchaseId = @PurchaseId
END