CREATE PROCEDURE [dbo].[Load_LastOrder_By_Location]
	@LocationId INT
AS
BEGIN
	SELECT TOP 1
		*
	FROM
		[Order]
	WHERE
		[LocationId] = @LocationId
	ORDER BY
		[Id] DESC;
END