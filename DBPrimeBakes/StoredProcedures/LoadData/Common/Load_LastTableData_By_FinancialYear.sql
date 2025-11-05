CREATE PROCEDURE [dbo].[Load_LastTableData_By_Company_FinancialYear]
	@TableName varchar(50),
	@CompanyId INT,
	@FinancialYearId INT
AS
BEGIN
	SET NOCOUNT ON;
	DECLARE @SQL nvarchar(MAX)
	SET @sql = N'SELECT TOP 1 * FROM ' + QUOTENAME(@TableName) + ' WHERE FinancialYearId = @FinancialYearId AND CompanyId = @CompanyId ORDER BY Id DESC';
	EXEC sp_executesql @sql,
					N'@FinancialYearId INT, @CompanyId INT',
					@FinancialYearId = @FinancialYearId,
					@CompanyId = @CompanyId;
END