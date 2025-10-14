CREATE PROCEDURE [dbo].[Load_SaleReturn_By_BillNo]
	@BillNo VARCHAR(20)
AS
BEGIN
	SELECT *
	FROM [dbo].[SaleReturn]
	WHERE BillNo = @BillNo;
END