CREATE PROCEDURE [dbo].[Load_LastAccounting_By_FinancialYear_Voucher]
	@FinancialYearId INT,
	@VoucherId INT
AS
BEGIN
	SELECT TOP 1 *
	FROM Accounting
	WHERE FinancialYearId = @FinancialYearId
	AND VoucherId = @VoucherId
	ORDER BY AccountingDate DESC, Id DESC
END