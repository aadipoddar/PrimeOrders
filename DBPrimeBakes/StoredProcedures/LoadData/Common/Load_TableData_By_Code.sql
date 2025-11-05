CREATE PROCEDURE [dbo].[Load_TableData_By_Code]
	@TableName varchar(50),
	@Code varchar(50)
AS
BEGIN
	SET NOCOUNT ON;
	DECLARE @SQL nvarchar(MAX)
	SET @sql = N'SELECT * FROM ' + QUOTENAME(@TableName) + ' WHERE Code = @Code';
	EXEC sp_executesql @sql,
					N'@Code VARCHAR(50)', 
					@Code = @Code;
END