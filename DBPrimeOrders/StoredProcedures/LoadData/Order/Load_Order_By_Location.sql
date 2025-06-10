CREATE PROCEDURE [dbo].[Load_Order_By_Location]
	@LocationId INT
AS
BEGIN
	SELECT
		*
	FROM [dbo].[Order] o
	WHERE o.LocationId = @LocationId
		AND o.Completed = 0
		AND o.Status = 1
END