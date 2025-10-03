CREATE PROCEDURE [dbo].[Load_SaleReturn_By_TransactionNo]
	@TransactionNo VARCHAR(20)
AS
BEGIN
	SELECT *
	FROM [dbo].[SaleReturn]
	WHERE TransactionNo = @TransactionNo;
END