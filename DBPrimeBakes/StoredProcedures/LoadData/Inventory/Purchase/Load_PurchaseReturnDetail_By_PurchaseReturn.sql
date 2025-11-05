CREATE PROCEDURE [dbo].[Load_PurchaseReturnDetail_By_PurchaseReturn]
	@PurchaseReturnId INT
AS
BEGIN
	SELECT *
	FROM [dbo].[PurchaseReturnDetail]
	WHERE [PurchaseReturnId] = @PurchaseReturnId
	AND [Status] = 1
END