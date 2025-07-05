CREATE PROCEDURE [dbo].[Load_RawMaterial_Rate_By_Supplier]
	@SupplierId INT
AS
BEGIN
	SELECT
		r.[Id],
		r.[Name],
		r.[Code],
		r.[RawMaterialCategoryId],
	
		ISNULL(
			CASE 
				WHEN @SupplierId > 0 THEN
					(SELECT TOP 1 Rate FROM PurchaseDetail pd
					 INNER JOIN Purchase p ON pd.PurchaseId = p.Id
					 WHERE pd.RawMaterialId = r.[Id]
					   AND p.SupplierId = @SupplierId
					   AND p.Status = 1
					   AND pd.Status = 1
					 ORDER BY pd.Id DESC)
				ELSE
					(SELECT TOP 1 Rate FROM PurchaseDetail pd
					 INNER JOIN Purchase p ON pd.PurchaseId = p.Id
					 WHERE pd.RawMaterialId = r.[Id]
					   AND p.Status = 1
					   AND pd.Status = 1
					 ORDER BY pd.Id DESC)
			END, r.[MRP]) AS [MRP],
	
		r.[TaxId],
		r.[Status]
	FROM RawMaterial r
	WHERE r.[Status] = 1
END