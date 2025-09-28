CREATE PROCEDURE [dbo].[Load_LastSaleReturn_By_Location]
	@LocationId INT
AS
BEGIN
	SELECT TOP 1
		*
	FROM
		SaleReturn
	WHERE
		[LocationId] = @LocationId
	ORDER BY
		[Id] DESC;
END