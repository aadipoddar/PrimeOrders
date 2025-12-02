CREATE PROCEDURE [dbo].[Load_TableData_By_Date]
	@TableName varchar(50),
	@StartDate DATETIME,
	@EndDate DATETIME
AS
BEGIN
	SET NOCOUNT ON;
	DECLARE @SQL nvarchar(MAX)
	SET @sql = N'SELECT * FROM ' + QUOTENAME(@TableName) + ' WHERE TransactionDateTime BETWEEN @StartDate AND @EndDate';
	EXEC sp_executesql @sql,
					N'@StartDate DATETIME, @EndDate DATETIME',
					@StartDate = @StartDate,
					@EndDate = @EndDate;
END