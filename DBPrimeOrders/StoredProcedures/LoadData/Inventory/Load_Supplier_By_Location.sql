CREATE PROCEDURE [dbo].[Load_Supplier_By_Location]
	@LocationId INT
AS
BEGIN
	SELECT *
	FROM Supplier
	WHERE LocationId = @LocationId;
END