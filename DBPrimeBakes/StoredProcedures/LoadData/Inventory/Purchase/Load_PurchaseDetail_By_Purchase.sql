CREATE PROCEDURE [dbo].[Load_PurchaseDetail_By_Purchase]
	@PurchaseId INT
AS
BEGIN
	SELECT
		*
	FROM PurchaseDetail
	WHERE PurchaseId = @PurchaseId
		AND Status = 1
END