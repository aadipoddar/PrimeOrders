CREATE PROCEDURE [dbo].[Load_Accounting_By_ReferenceNo]
	@ReferenceNo VARCHAR(20)
AS
BEGIN
	SELECT *
	FROM Accounting
	WHERE ReferenceNo = @ReferenceNo
	AND Status = 1
END