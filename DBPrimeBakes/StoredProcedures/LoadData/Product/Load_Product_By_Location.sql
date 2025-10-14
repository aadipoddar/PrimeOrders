CREATE PROCEDURE [dbo].[Load_Product_By_Location]
	@LocationId INT
AS
BEGIN
	SELECT
		*
	FROM
	ProductLocation_Overview
	WHERE LocationId = @LocationId
END