CREATE PROCEDURE [dbo].[Load_Sale_By_BillNo]
	@BillNo VARCHAR(20)
AS
BEGIN
	SELECT *
	FROM [dbo].[Sale]
	WHERE BillNo = @BillNo;
END