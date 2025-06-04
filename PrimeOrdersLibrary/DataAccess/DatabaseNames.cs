namespace PrimeOrdersLibrary.DataAccess;

public static class TableNames
{
	public static string User => "User";
	public static string Location => "Location";
	public static string Tax => "Tax";
	public static string ProductCategory => "ProductCategory";
	public static string Product => "Product";
	public static string RawMaterialCategory => "RawMaterialCategory";
	public static string RawMaterial => "RawMaterial";
	public static string Recipe => "Recipe";
	public static string RecipeDetail => "RecipeDetail";
}

public static class StoredProcedureNames
{
	public static string LoadTableData => "Load_TableData";
	public static string LoadTableDataById => "Load_TableData_By_Id";
	public static string LoadTableDataByStatus => "Load_TableData_By_Status";

	public static string LoadUserByPasscode => "Load_User_By_Passcode";

	public static string LoadRawMaterialByRawMaterialCategory => "Load_RawMaterial_By_RawMaterialCategory";
	public static string LoadRecipeByProduct => "Load_Recipe_By_Product";
	public static string LoadRecipeDetailByRecipe => "Load_RecipeDetail_By_Recipe";

	public static string LoadProductByProductCategory => "Load_Product_By_ProductCategory";

	public static string InsertUser => "Insert_User";
	public static string InsertLocation => "Insert_Location";

	public static string InsertTax => "Insert_Tax";

	public static string InsertProductCategory => "Insert_ProductCategory";
	public static string InsertProduct => "Insert_Product";

	public static string InsertRawMaterialCategory => "Insert_RawMaterialCategory";
	public static string InsertRawMaterial => "Insert_RawMaterial";

	public static string InsertRecipe => "Insert_Recipe";
	public static string InsertRecipeDetail => "Insert_RecipeDetail";

}

public static class ViewNames
{
}