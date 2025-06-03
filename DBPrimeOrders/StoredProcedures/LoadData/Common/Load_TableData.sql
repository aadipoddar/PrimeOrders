CREATE PROCEDURE [dbo].[Load_TableData]
	@TableName varchar(50)
AS
BEGIN
	SET NOCOUNT ON;
	DECLARE @SQL nvarchar(MAX)
	SET @sql = N'SELECT * FROM ' + QUOTENAME(@TableName);
	EXEC sp_executesql @SQL
END