namespace PrimeBakesLibrary.DataAccess;

public static class TableNames
{
	public static string User => "User";
	public static string Location => "Location";
	public static string State => "State";
	public static string Settings => "Settings";
	public static string Tax => "Tax";
	public static string ProductCategory => "ProductCategory";
	public static string Product => "Product";
	public static string ProductRate => "ProductRate";
	public static string RawMaterialCategory => "RawMaterialCategory";
	public static string RawMaterial => "RawMaterial";
	public static string Recipe => "Recipe";
	public static string RecipeDetail => "RecipeDetail";
	public static string Purchase => "Purchase";
	public static string PurchaseDetail => "PurchaseDetail";
	public static string ProductStock => "ProductStock";
	public static string RawMaterialStock => "RawMaterialStock";
	public static string Order => "Order";
	public static string OrderDetail => "OrderDetail";
	public static string Customer => "Customer";
	public static string Sale => "Sale";
	public static string SaleDetail => "SaleDetail";
	public static string SaleReturn => "SaleReturn";
	public static string SaleReturnDetail => "SaleReturnDetail";
	public static string Kitchen => "Kitchen";
	public static string KitchenIssue => "KitchenIssue";
	public static string KitchenIssueDetail => "KitchenIssueDetail";
	public static string KitchenProduction => "KitchenProduction";
	public static string KitchenProductionDetail => "KitchenProductionDetail";
	public static string Ledger => "Ledger";
	public static string Group => "Group";
	public static string AccountType => "AccountType";
	public static string Voucher => "Voucher";
	public static string FinancialYear => "FinancialYear";
	public static string Accounting => "Accounting";
	public static string AccountingDetails => "AccountingDetails";
}

public static class StoredProcedureNames
{
	public static string LoadTableData => "Load_TableData";
	public static string LoadTableDataById => "Load_TableData_By_Id";
	public static string LoadTableDataByStatus => "Load_TableData_By_Status";
	public static string LoadSettingsByKey => "Load_Settings_By_Key";
	public static string ResetSettings => "Reset_Settings";
	public static string UpdateSettings => "Update_Settings";

	public static string LoadUserByPasscode => "Load_User_By_Passcode";
	public static string LoadCustomerByNumber => "Load_Customer_By_Number";

	public static string LoadRawMaterialByRawMaterialCategory => "Load_RawMaterial_By_RawMaterialCategory";
	public static string LoadRawMaterialRateBySupplierPurchaseDateTime => "Load_RawMaterial_Rate_By_Supplier_PurchaseDateTime";

	public static string LoadRecipeByProduct => "Load_Recipe_By_Product";
	public static string LoadRecipeDetailByRecipe => "Load_RecipeDetail_By_Recipe";

	public static string LoadPurchaseDetailByPurchase => "Load_PurchaseDetail_By_Purchase";
	public static string LoadPurchaseOverviewByPurchaseId => "Load_PurchaseOverview_By_PurchaseId";

	public static string LoadProductByProductCategory => "Load_Product_By_ProductCategory";
	public static string LoadProductRateByProduct => "Load_ProductRate_By_Product";
	public static string LoadProductByLocationRate => "Load_Product_By_LocationRate";

	public static string LoadOrderDetailByOrder => "Load_OrderDetail_By_Order";
	public static string LoadOrderByLocation => "Load_Order_By_Location";
	public static string LoadLastOrderByLocation => "Load_LastOrder_By_Location";
	public static string LoadOrderBySale => "Load_Order_By_Sale";
	public static string LoadOrderOverviewByOrderId => "Load_OrderOverview_By_OrderId";
	public static string LoadOrderByOrderNo => "Load_Order_By_OrderNo";

	public static string LoadSaleDetailBySale => "Load_SaleDetail_By_Sale";
	public static string LoadLastSaleByLocation => "Load_LastSale_By_Location";
	public static string LoadSaleOverviewBySaleId => "Load_SaleOverview_By_SaleId";
	public static string LoadSaleByBillNo => "Load_Sale_By_BillNo";

	public static string LoadSaleReturnDetailBySaleReturn => "Load_SaleReturnDetail_By_SaleReturn";
	public static string LoadLastSaleReturnByLocation => "Load_LastSaleReturn_By_Location";
	public static string LoadSaleReturnBySale => "Load_SaleReturn_By_Sale";
	public static string LoadSaleReturnOverviewBySaleReturnId => "Load_SaleReturnOverview_By_SaleReturnId";
	public static string LoadSaleReturnByTransactionNo => "Load_SaleReturn_By_TransactionNo";

	public static string LoadLastKitchenIssueByLocation => "Load_LastKitchenIssue_By_Location";
	public static string LoadKitchenIssueDetailByKitchenIssue => "Load_KitchenIssueDetail_By_KitchenIssue";
	public static string LoadKitchenIssueOverviewByKitchenIssueId => "Load_KitchenIssueOverview_By_KitchenIssueId";
	public static string LoadKitchenIssueByTransactionNo => "Load_KitchenIssue_By_TransactionNo";

	public static string LoadLastKitchenProductionByLocation => "Load_LastKitchenProduction_By_Location";
	public static string LoadKitchenProductionDetailByKitchenProduction => "Load_KitchenProductionDetail_By_KitchenProduction";
	public static string LoadKitchenProductionByTransactionNo => "Load_KitchenProduction_By_TransactionNo";

	public static string LoadSaleDetailsByDateLocationId => "Load_SaleDetails_By_Date_LocationId";
	public static string LoadOrderDetailsByDateLocationId => "Load_OrderDetails_By_Date_LocationId";
	public static string LoadProductDetailsByDateLocationId => "Load_ProductDetails_By_Date_LocationId";
	public static string LoadRawMaterialStockDetailsByDateLocationId => "Load_RawMaterialStockDetails_By_Date_LocationId";
	public static string LoadRawMaterialStockSummaryByDateLocationId => "Load_RawMaterialStockSummary_By_Date_LocationId";
	public static string LoadProductStockSummaryByDateLocationId => "Load_ProductStockSummary_By_Date_LocationId";
	public static string LoadProductStockDetailsByDateLocationId => "Load_ProductStockDetails_By_Date_LocationId";
	public static string LoadPurchaseDetailsByDate => "Load_PurchaseDetails_By_Date";
	public static string LoadKitchenIssueDetailsByDate => "Load_KitchenIssueDetails_By_Date";
	public static string LoadKitchenProductionDetailsByDate => "Load_KitchenProductionDetails_By_Date";
	public static string LoadSaleReturnDetailsByDateLocationId => "Load_SaleReturnDetails_By_Date_LocationId";

	public static string LoadLedgerByLocation => "Load_Ledger_By_Location";
	public static string LoadFinancialYearByDate => "Load_FinancialYear_By_Date";

	public static string LoadLastAccountingByFinancialYearVoucher => "Load_LastAccounting_By_FinancialYear_Voucher";
	public static string LoadAccountingDetailsByAccounting => "Load_AccountingDetails_By_Accounting";
	public static string LoadAccountingByReferenceNo => "Load_Accounting_By_ReferenceNo";

	public static string LoadAccountingDetailsByDate => "Load_AccountingDetails_By_Date";
	public static string LoadAccountingOverviewByAccountingId => "Load_AccountingOverview_By_AccountingId";

	public static string LoadLedgerDetailsByDateLedger => "Load_LedgerDetails_By_Date_Ledger";
	public static string LoadTrialBalanceByDate => "Load_TrialBalance_By_Date";

	public static string InsertUser => "Insert_User";
	public static string InsertLocation => "Insert_Location";
	public static string InsertCustomer => "Insert_Customer";

	public static string InsertTax => "Insert_Tax";

	public static string InsertKitchen => "Insert_Kitchen";

	public static string InsertProductCategory => "Insert_ProductCategory";
	public static string InsertProduct => "Insert_Product";
	public static string InsertProductRate => "Insert_ProductRate";

	public static string InsertRawMaterialCategory => "Insert_RawMaterialCategory";
	public static string InsertRawMaterial => "Insert_RawMaterial";

	public static string InsertRecipe => "Insert_Recipe";
	public static string InsertRecipeDetail => "Insert_RecipeDetail";

	public static string InsertPurchase => "Insert_Purchase";
	public static string InsertPurchaseDetail => "Insert_PurchaseDetail";

	public static string InsertProductStock => "Insert_ProductStock";
	public static string InsertRawMaterialStock => "Insert_RawMaterialStock";

	public static string InsertOrder => "Insert_Order";
	public static string InsertOrderDetail => "Insert_OrderDetail";

	public static string InsertSale => "Insert_Sale";
	public static string InsertSaleDetail => "Insert_SaleDetail";
	public static string InsertSaleReturn => "Insert_SaleReturn";
	public static string InsertSaleReturnDetail => "Insert_SaleReturnDetail";

	public static string InsertKitchenIssue => "Insert_KitchenIssue";
	public static string InsertKitchenIssueDetail => "Insert_KitchenIssueDetail";

	public static string InsertKitchenProduction => "Insert_KitchenProduction";
	public static string InsertKitchenProductionDetail => "Insert_KitchenProductionDetail";

	public static string InsertLedger => "Insert_Ledger";
	public static string InsertGroup => "Insert_Group";
	public static string InsertAccountType => "Insert_AccountType";
	public static string InsertVoucher => "Insert_Voucher";
	public static string InsertFinancialYear => "Insert_FinancialYear";

	public static string InsertAccounting => "Insert_Accounting";
	public static string InsertAccountingDetails => "Insert_AccountingDetails";

	public static string DeleteProductStockByTransactionNo => "Delete_ProductStock_By_TransactionNo";
	public static string DeleteRawMaterialStockByTransactionNo => "Delete_RawMaterialStock_By_TransactionNo";
}

public static class ViewNames
{
	public static string SaleOverview => "Sale_Overview";
	public static string SaleReturnOverview => "SaleReturn_Overview";
	public static string OrderOverview => "Order_Overview";
	public static string ProductOverview => "Product_Overview";
	public static string PurchaseOverview => "Purchase_Overview";
	public static string KitchenIssueOverview => "KitchenIssue_Overview";
	public static string KitchenProductionOverview => "KitchenProduction_Overview";
	public static string AccountingOverview => "Accounting_Overview";
	public static string LedgerOverview => "Ledger_Overview";
	public static string RawMaterialStockDetails => "RawMaterialStockDetails";
}