using PrimeOrdersLibrary.Models.Product;

namespace PrimeOrdersLibrary.Data.Product;

public static class ProductData
{
	public static async Task InsertProduct(ProductModel product) =>
			await SqlDataAccess.SaveData(StoredProcedureNames.InsertProduct, product);

	public static async Task InsertProductCategory(ProductCategoryModel productCategory) =>
			await SqlDataAccess.SaveData(StoredProcedureNames.InsertProductCategory, productCategory);

	public static async Task<List<ProductModel>> LoadProductByProductCategory(int ProductCategoryId) =>
			await SqlDataAccess.LoadData<ProductModel, dynamic>(StoredProcedureNames.LoadProductByProductCategory, new { ProductCategoryId });

	public static async Task<List<ProductOverviewModel>> LoadProductDetailsByDateLocationId(DateTime FromDate, DateTime ToDate, int LocationId) =>
		await SqlDataAccess.LoadData<ProductOverviewModel, dynamic>(StoredProcedureNames.LoadProductDetailsByDateLocationId, new { FromDate, ToDate, LocationId });
}