CREATE PROCEDURE [dbo].[Load_PurchaseDetail_By_Purchase]
	@PurchaseId INT
AS
BEGIN
	SELECT *
	FROM [dbo].[PurchaseDetail]
	WHERE [PurchaseId] = @PurchaseId
	AND [Status] = 1
END