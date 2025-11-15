CREATE PROCEDURE [dbo].[Load_KitchenProductionDetail_By_KitchenProduction]
	@KitchenProductionId INT
AS
BEGIN
	SELECT *
	FROM [dbo].[KitchenProductionDetail]
	WHERE [KitchenProductionId] = @KitchenProductionId
	AND [Status] = 1
END
