CREATE PROCEDURE [dbo].[Load_Order_By_Completed]
	@Completed BIT
AS
BEGIN
	SELECT *
	FROM [Order]
	WHERE Completed = @Completed
		AND [Status] = 1
END