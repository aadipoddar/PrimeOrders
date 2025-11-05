CREATE PROCEDURE [dbo].[Load_TableData_By_TransactionNo]
	@TableName varchar(50),
	@TransactionNo varchar(50)
AS
BEGIN
	SET NOCOUNT ON;
	DECLARE @SQL nvarchar(MAX)
	SET @sql = N'SELECT * FROM ' + QUOTENAME(@TableName) + ' WHERE TransactionNo = @TransactionNo';
	EXEC sp_executesql @sql,
					N'@TransactionNo varchar(50)', 
					@TransactionNo = @TransactionNo;
END