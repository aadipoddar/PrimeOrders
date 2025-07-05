using PrimeOrdersLibrary.Models.Inventory;

namespace PrimeOrdersLibrary.Data.Inventory;

public static class RawMaterialData
{
	public static async Task InsertRawMaterial(RawMaterialModel rawMaterial) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertRawMaterial, rawMaterial);

	public static async Task InsertRawMaterialCategory(RawMaterialCategoryModel rawMaterialCategoryModel) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertRawMaterialCategory, rawMaterialCategoryModel);

	public static async Task<List<RawMaterialModel>> LoadRawMaterialByRawMaterialCategory(int RawMaterialCategoryId) =>
		await SqlDataAccess.LoadData<RawMaterialModel, dynamic>(StoredProcedureNames.LoadRawMaterialByRawMaterialCategory, new { RawMaterialCategoryId });

	public static async Task<List<RawMaterialModel>> LoadRawMaterialRateBySupplier(int SupplierId) =>
		await SqlDataAccess.LoadData<RawMaterialModel, dynamic>(StoredProcedureNames.LoadRawMaterialRateBySupplier, new { SupplierId });
}