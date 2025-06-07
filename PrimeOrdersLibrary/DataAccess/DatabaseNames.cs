namespace PrimeOrdersLibrary.DataAccess;

public static class TableNames
{
	public static string User => "User";
	public static string Location => "Location";
	public static string State => "State";
	public static string Tax => "Tax";
	public static string ProductCategory => "ProductCategory";
	public static string Product => "Product";
	public static string RawMaterialCategory => "RawMaterialCategory";
	public static string RawMaterial => "RawMaterial";
	public static string Recipe => "Recipe";
	public static string RecipeDetail => "RecipeDetail";
	public static string Supplier => "Supplier";
	public static string Purchase => "Purchase";
	public static string PurchaseDetail => "PurchaseDetail";
	public static string Stock => "Stock";
	public static string Order => "Order";
	public static string OrderDetail => "OrderDetail";
	public static string Sale => "Sale";
	public static string SaleDetail => "SaleDetail";
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
	public static string LoadPurchaseDetailByPurchase => "Load_PurchaseDetail_By_Purchase";

	public static string LoadOrderDetailByOrder => "Load_OrderDetail_By_Order";
	public static string LoadOrderByCompleted => "Load_Order_By_Completed";

	public static string LoadSaleDetailBySale => "Load_SaleDetail_By_Sale";

	public static string InsertUser => "Insert_User";
	public static string InsertLocation => "Insert_Location";

	public static string InsertTax => "Insert_Tax";

	public static string InsertProductCategory => "Insert_ProductCategory";
	public static string InsertProduct => "Insert_Product";

	public static string InsertRawMaterialCategory => "Insert_RawMaterialCategory";
	public static string InsertRawMaterial => "Insert_RawMaterial";

	public static string InsertRecipe => "Insert_Recipe";
	public static string InsertRecipeDetail => "Insert_RecipeDetail";

	public static string InsertSupplier => "Insert_Supplier";

	public static string InsertPurchase => "Insert_Purchase";
	public static string InsertPurchaseDetail => "Insert_PurchaseDetail";

	public static string InsertStock => "Insert_Stock";

	public static string InsertOrder => "Insert_Order";
	public static string InsertOrderDetail => "Insert_OrderDetail";

	public static string InsertSale => "Insert_Sale";
	public static string InsertSaleDetail => "Insert_SaleDetail";
}

public static class ViewNames
{
}