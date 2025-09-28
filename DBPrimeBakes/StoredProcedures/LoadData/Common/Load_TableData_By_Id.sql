CREATE PROCEDURE [dbo].[Load_TableData_By_Id]
	@TableName varchar(50),
	@Id int
AS
BEGIN
	SET NOCOUNT ON;
	DECLARE @SQL nvarchar(MAX)
	SET @sql = N'SELECT * FROM ' + QUOTENAME(@TableName) + ' WHERE Id = @Id';
	EXEC sp_executesql @sql,
					N'@Id int', 
					@Id = @Id
END