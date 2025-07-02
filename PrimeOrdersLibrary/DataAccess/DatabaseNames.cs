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
	public static string ProductStock => "ProductStock";
	public static string RawMaterialStock => "RawMaterialStock";
	public static string Order => "Order";
	public static string OrderDetail => "OrderDetail";
	public static string Sale => "Sale";
	public static string SaleDetail => "SaleDetail";
	public static string Kitchen => "Kitchen";
	public static string KitchenIssue => "KitchenIssue";
	public static string KitchenIssueDetail => "KitchenIssueDetail";
	public static string KitchenProduction => "KitchenProduction";
	public static string KitchenProductionDetail => "KitchenProductionDetail";
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
	public static string LoadOrderByLocation => "Load_Order_By_Location";
	public static string LoadLastOrderByLocation => "Load_LastOrder_By_Location";
	public static string LoadOrderBySale => "Load_Order_By_Sale";

	public static string LoadSaleDetailBySale => "Load_SaleDetail_By_Sale";
	public static string LoadLastSaleByLocation => "Load_LastSale_By_Location";

	public static string LoadLastKitchenIssueByLocation => "Load_LastKitchenIssue_By_Location";
	public static string LoadLastKitchenProductionByLocation => "Load_LastKitchenProduction_By_Location";
	public static string LoadKitchenIssueDetailByKitchenIssue => "Load_KitchenIssueDetail_By_KitchenIssue";
	public static string LoadKitchenProductionDetailByKitchenProduction => "Load_KitchenProductionDetail_By_KitchenProduction";

	public static string LoadSaleDetailsByDateLocationId => "Load_SaleDetails_By_Date_LocationId";
	public static string LoadOrderDetailsByDateLocationId => "Load_OrderDetails_By_Date_LocationId";
	public static string LoadProductDetailsByDateLocationId => "Load_ProductDetails_By_Date_LocationId";
	public static string LoadRawMaterialStockDetailsByDateLocationId => "Load_RawMaterialStockDetails_By_Date_LocationId";
	public static string LoadProductStockDetailsByDateLocationId => "Load_ProductStockDetails_By_Date_LocationId";
	public static string LoadPurchaseDetailsByDate => "Load_PurchaseDetails_By_Date";
	public static string LoadKitchenIssueDetailsByDate => "Load_KitchenIssueDetails_By_Date";
	public static string LoadKitchenProductionDetailsByDate => "Load_KitchenProductionDetails_By_Date";

	public static string InsertUser => "Insert_User";
	public static string InsertLocation => "Insert_Location";

	public static string InsertTax => "Insert_Tax";

	public static string InsertKitchen => "Insert_Kitchen";

	public static string InsertProductCategory => "Insert_ProductCategory";
	public static string InsertProduct => "Insert_Product";

	public static string InsertRawMaterialCategory => "Insert_RawMaterialCategory";
	public static string InsertRawMaterial => "Insert_RawMaterial";

	public static string InsertRecipe => "Insert_Recipe";
	public static string InsertRecipeDetail => "Insert_RecipeDetail";

	public static string InsertSupplier => "Insert_Supplier";

	public static string InsertPurchase => "Insert_Purchase";
	public static string InsertPurchaseDetail => "Insert_PurchaseDetail";

	public static string InsertProductStock => "Insert_ProductStock";
	public static string InsertRawMaterialStock => "Insert_RawMaterialStock";

	public static string InsertOrder => "Insert_Order";
	public static string InsertOrderDetail => "Insert_OrderDetail";

	public static string InsertSale => "Insert_Sale";
	public static string InsertSaleDetail => "Insert_SaleDetail";

	public static string InsertKitchenIssue => "Insert_KitchenIssue";
	public static string InsertKitchenIssueDetail => "Insert_KitchenIssueDetail";

	public static string InsertKitchenProduction => "Insert_KitchenProduction";
	public static string InsertKitchenProductionDetail => "Insert_KitchenProductionDetail";

	public static string DeleteProductStockByTransactionNo => "Delete_ProductStock_By_TransactionNo";
	public static string DeleteRawMaterialStockByTransactionNo => "Delete_RawMaterialStock_By_TransactionNo";
}

public static class ViewNames
{
	public static string SaleOverview => "Sale_Overview";
	public static string OrderOverview => "Order_Overview";
	public static string ProductOverview => "Product_Overview";
	public static string PurchaseOverview => "Purchase_Overview";
	public static string KitchenIssueOverview => "KitchenIssue_Overview";
	public static string KitchenProductionOverview => "KitchenProduction_Overview";
}