CREATE PROCEDURE [dbo].[Load_KitchenProductionDetail_By_KitchenProduction]
	@KitchenProductionId INT
AS
BEGIN
	SELECT
		*
	FROM [dbo].[KitchenProductionDetail] AS kpd
	WHERE kpd.KitchenProductionId = @KitchenProductionId
	AND kpd.Status = 1
END