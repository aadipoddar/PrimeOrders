CREATE PROCEDURE [dbo].[Load_RawMaterial_By_Party_PurchaseDateTime]
	@PartyId INT,
	@PurchaseDateTime DATETIME,
	@OnlyActive BIT
AS
BEGIN
	SELECT
		r.[Id],
		r.[Name],
		r.[Code],
		r.[RawMaterialCategoryId],

		ISNULL(
			CASE 
				WHEN @PartyId > 0 THEN
					(SELECT TOP 1 Rate FROM PurchaseDetail pd
					 INNER JOIN Purchase p ON pd.PurchaseId = p.Id
					 WHERE pd.RawMaterialId = r.[Id]
					   AND p.PartyId = @PartyId
					   AND p.Status = 1
					   AND pd.Status = 1
					   AND p.TransactionDateTime <= @PurchaseDateTime
					 ORDER BY pd.Id DESC)
				ELSE
					(SELECT TOP 1 Rate FROM PurchaseDetail pd
					 INNER JOIN Purchase p ON pd.PurchaseId = p.Id
					 WHERE pd.RawMaterialId = r.[Id]
					   AND p.Status = 1
					   AND pd.Status = 1
					   AND p.TransactionDateTime <= @PurchaseDateTime
					 ORDER BY pd.Id DESC)
			END, r.[Rate]) AS [Rate],

		ISNULL(
			CASE 
				WHEN @PartyId > 0 THEN
					(SELECT TOP 1 UnitOfMeasurement FROM PurchaseDetail pd
					 INNER JOIN Purchase p ON pd.PurchaseId = p.Id
					 WHERE pd.RawMaterialId = r.[Id]
					   AND p.PartyId = @PartyId
					   AND p.Status = 1
					   AND pd.Status = 1
					   AND p.TransactionDateTime <= @PurchaseDateTime
					   ORDER BY pd.Id DESC)
				ELSE
					(SELECT TOP 1 UnitOfMeasurement FROM PurchaseDetail pd
					 INNER JOIN Purchase p ON pd.PurchaseId = p.Id
					 WHERE pd.RawMaterialId= r.[Id]
					   AND p.Status = 1
					   AND pd.Status = 1
					   AND p.TransactionDateTime <= @PurchaseDateTime
					   ORDER BY pd.Id DESC)
		END, r.[UnitOfMeasurement]) AS [UnitOfMeasurement],

		r.[TaxId],
		r.[Status]

	FROM RawMaterial r
	WHERE (@OnlyActive = 0 OR r.[Status] = 1)
END