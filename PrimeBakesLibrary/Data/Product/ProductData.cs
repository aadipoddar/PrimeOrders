using PrimeBakesLibrary.Models.Product;

namespace PrimeBakesLibrary.Data.Product;

public static class ProductData
{
	public static async Task<int> InsertProduct(ProductModel product) =>
			(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertProduct, product)).FirstOrDefault();

	public static async Task InsertProductCategory(ProductCategoryModel productCategory) =>
			await SqlDataAccess.SaveData(StoredProcedureNames.InsertProductCategory, productCategory);

	public static async Task InsertProductLocation(ProductLocationModel productLocation) =>
			await SqlDataAccess.SaveData(StoredProcedureNames.InsertProductLocation, productLocation);

	public static async Task<List<ProductLocationOverviewModel>> LoadProductRateByProduct(int ProductId) =>
			await SqlDataAccess.LoadData<ProductLocationOverviewModel, dynamic>(StoredProcedureNames.LoadProductRateByProduct, new { ProductId });

	public static async Task<List<ProductLocationOverviewModel>> LoadProductByLocation(int LocationId) =>
			await SqlDataAccess.LoadData<ProductLocationOverviewModel, dynamic>(StoredProcedureNames.LoadProductByLocation, new { LocationId });

	public static async Task<List<ProductModel>> LoadProductByProductCategory(int ProductCategoryId) =>
			await SqlDataAccess.LoadData<ProductModel, dynamic>(StoredProcedureNames.LoadProductByProductCategory, new { ProductCategoryId });

	public static async Task<List<ProductOverviewModel>> LoadProductDetailsByDateLocationId(DateTime FromDate, DateTime ToDate, int LocationId) =>
			await SqlDataAccess.LoadData<ProductOverviewModel, dynamic>(StoredProcedureNames.LoadProductDetailsByDateLocationId, new { FromDate, ToDate, LocationId });
}