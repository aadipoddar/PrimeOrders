CREATE PROCEDURE [dbo].[Load_LastKitchenIssue_By_Location]
	@LocationId INT
AS
BEGIN
	SELECT TOP 1
		*
	FROM
		KitchenIssue
	WHERE
		[LocationId] = @LocationId
	ORDER BY
		[Id] DESC;
END