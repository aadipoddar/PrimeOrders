CREATE PROCEDURE [dbo].[Load_LastTableData_By_Location_FinancialYear]
	@TableName varchar(50),
	@LocationId INT,
	@FinancialYearId INT
AS
BEGIN
	SET NOCOUNT ON;
	DECLARE @SQL nvarchar(MAX)
	SET @sql = N'SELECT TOP 1 * FROM ' + QUOTENAME(@TableName) + ' WHERE FinancialYearId = @FinancialYearId AND LocationId = @LocationId ORDER BY Id DESC';
	EXEC sp_executesql @sql,
					N'@FinancialYearId INT, @LocationId INT',
					@FinancialYearId = @FinancialYearId,
					@LocationId = @LocationId;
END