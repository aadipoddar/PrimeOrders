CREATE PROCEDURE [dbo].[Load_RawMaterial_By_RawMaterialCategory]
	@RawMaterialCategoryId INT
AS
BEGIN
	SELECT *
	FROM dbo.RawMaterial
	WHERE RawMaterialCategoryId = @RawMaterialCategoryId;
END