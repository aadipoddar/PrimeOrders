CREATE PROCEDURE [dbo].[Load_LastSale_By_Location]
	@LocationId INT
AS
BEGIN
	SELECT TOP 1
		*
	FROM
		Sale
	WHERE
		[LocationId] = @LocationId
	ORDER BY
		[Id] DESC;
END