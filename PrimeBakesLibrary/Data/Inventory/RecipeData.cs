using PrimeBakesLibrary.Models.Inventory;

namespace PrimeBakesLibrary.Data.Inventory;

public static class RecipeData
{
	public static async Task<int> InsertRecipe(RecipeModel recipeModel) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertRecipe, recipeModel)).FirstOrDefault();

	public static async Task<int> InsertRecipeDetail(RecipeDetailModel recipeDetailModel) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertRecipeDetail, recipeDetailModel)).FirstOrDefault();

	public static async Task<RecipeModel> LoadRecipeByProduct(int ProductId) =>
		(await SqlDataAccess.LoadData<RecipeModel, dynamic>(StoredProcedureNames.LoadRecipeByProduct, new { ProductId })).FirstOrDefault();

	public static async Task<List<RecipeDetailModel>> LoadRecipeDetailByRecipe(int RecipeId) =>
		await SqlDataAccess.LoadData<RecipeDetailModel, dynamic>(StoredProcedureNames.LoadRecipeDetailByRecipe, new { RecipeId });

	public static async Task<int> SaveRecipe(RecipeModel recipe, List<RecipeItemCartModel> cart)
	{
		bool update = recipe.Id > 0;

		if (update)
		{
			var recipeDetails = await LoadRecipeDetailByRecipe(recipe.Id);
			foreach (var detail in recipeDetails)
			{
				detail.Status = false;
				await InsertRecipeDetail(detail);
			}
		}

		recipe.Id = await InsertRecipe(recipe);

		foreach (var item in cart)
			await InsertRecipeDetail(new ()
			{
				Id = 0,
				RecipeId = recipe.Id,
				RawMaterialId = item.ItemId,
				Quantity = item.Quantity,
				Status = true
			});

		return recipe.Id;
	}
}
