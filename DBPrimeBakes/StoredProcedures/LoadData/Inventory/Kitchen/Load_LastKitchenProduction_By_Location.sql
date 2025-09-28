CREATE PROCEDURE [dbo].[Load_LastKitchenProduction_By_Location]
	@LocationId INT
AS
BEGIN
	SELECT TOP 1
		*
	FROM
		KitchenProduction
	WHERE
		[LocationId] = @LocationId
	ORDER BY
		[Id] DESC;
END