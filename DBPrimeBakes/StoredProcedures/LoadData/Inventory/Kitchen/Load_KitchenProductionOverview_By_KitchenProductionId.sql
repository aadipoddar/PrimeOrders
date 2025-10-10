CREATE PROCEDURE [dbo].[Load_KitchenProductionOverview_By_KitchenProductionId]
	@KitchenProductionId INT
AS
BEGIN
	SELECT *
	FROM [dbo].[KitchenProduction_Overview]
	WHERE KitchenProductionId = @KitchenProductionId
END