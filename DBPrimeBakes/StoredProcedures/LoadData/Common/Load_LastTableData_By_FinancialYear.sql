CREATE PROCEDURE [dbo].[Load_LastTableData_By_FinancialYear]
	@TableName varchar(50),
	@FinancialYearId INT
AS
BEGIN
	SET NOCOUNT ON;
	DECLARE @SQL nvarchar(MAX)
	SET @sql = N'SELECT TOP 1 * FROM ' + QUOTENAME(@TableName) + ' WHERE FinancialYearId = @FinancialYearId ORDER BY Id DESC';
	EXEC sp_executesql @sql,
					N'@FinancialYearId INT',
					@FinancialYearId = @FinancialYearId;
END