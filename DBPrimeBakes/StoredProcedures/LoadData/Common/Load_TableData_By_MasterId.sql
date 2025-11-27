CREATE PROCEDURE [dbo].[Load_TableData_By_MasterId]
	@TableName varchar(50),
	@MasterId int
AS
BEGIN
	SET NOCOUNT ON;
	DECLARE @SQL nvarchar(MAX)
	SET @sql = N'SELECT * FROM ' + QUOTENAME(@TableName) + ' WHERE MasterId = @MasterId AND Status = 1';
	EXEC sp_executesql @sql,
					N'@MasterId int', 
					@MasterId = @MasterId
END