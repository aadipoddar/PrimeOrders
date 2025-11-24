CREATE PROCEDURE [dbo].[Load_Accounting_By_Voucher_Reference]
	@VoucherId INT,
	@ReferenceId INT,
	@ReferenceNo VARCHAR(MAX)
AS
BEGIN
	SELECT *
	FROM [Accounting]
	WHERE VoucherId = @VoucherId
	AND ReferenceId = @ReferenceId
	AND ReferenceNo = @ReferenceNo;
END