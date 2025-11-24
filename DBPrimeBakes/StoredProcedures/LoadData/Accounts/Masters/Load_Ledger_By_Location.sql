CREATE PROCEDURE [dbo].[Load_Ledger_By_Location]
	@LocationId INT
AS
BEGIN
	SELECT *
	FROM [Ledger]
	WHERE LocationId = @LocationId;
END