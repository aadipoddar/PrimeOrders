using PrimeBakesLibrary.Models.Inventory;

namespace PrimeBakesLibrary.Data.Inventory;

public static class RecipeData
{
	public static async Task<int> InsertRecipe(RecipeModel recipeModel) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertRecipe, recipeModel)).FirstOrDefault();

	public static async Task InsertRecipeDetail(RecipeDetailModel recipeDetailModel) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertRecipeDetail, recipeDetailModel);

	public static async Task<RecipeModel> LoadRecipeByProduct(int ProductId) =>
		(await SqlDataAccess.LoadData<RecipeModel, dynamic>(StoredProcedureNames.LoadRecipeByProduct, new { ProductId })).FirstOrDefault();

	public static async Task<List<RecipeDetailModel>> LoadRecipeDetailByRecipe(int RecipeId) =>
		await SqlDataAccess.LoadData<RecipeDetailModel, dynamic>(StoredProcedureNames.LoadRecipeDetailByRecipe, new { RecipeId });
}
