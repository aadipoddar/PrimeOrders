CREATE PROCEDURE [dbo].[Load_KitchenProduction_By_TransactionNo]
	@TransactionNo VARCHAR(20)
AS
BEGIN
	SELECT *
	FROM [dbo].[KitchenProduction]
	WHERE TransactionNo = @TransactionNo;
END