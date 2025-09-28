CREATE PROCEDURE [dbo].[Load_TableData_By_Status]
	@TableName varchar(50),
	@Status bit
AS
BEGIN
	SET NOCOUNT ON;
	DECLARE @SQL nvarchar(MAX)
	SET @sql = N'SELECT * FROM ' + QUOTENAME(@TableName) + ' WHERE Status = @Status';
	EXEC sp_executesql @sql,
					N'@Status BIT', 
					@Status = @Status;
END