CREATE PROCEDURE [dbo].[Load_KitchenIssue_By_TransactionNo]
	@TransactionNo VARCHAR(20)
AS
BEGIN
	SELECT *
	FROM [dbo].[KitchenIssue]
	WHERE TransactionNo = @TransactionNo;
END