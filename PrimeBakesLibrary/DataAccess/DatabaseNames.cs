namespace PrimeBakesLibrary.DataAccess;

public static class TableNames
{
	public static string User => "User";
	public static string Location => "Location";
	public static string Company => "Company";
	public static string StateUT => "StateUT";
	public static string Settings => "Settings";
	public static string Tax => "Tax";
	public static string ProductCategory => "ProductCategory";
	public static string Product => "Product";
	public static string ProductLocation => "ProductLocation";
	public static string RawMaterialCategory => "RawMaterialCategory";
	public static string RawMaterial => "RawMaterial";
	public static string Recipe => "Recipe";
	public static string RecipeDetail => "RecipeDetail";
	public static string Purchase => "Purchase";
	public static string PurchaseDetail => "PurchaseDetail";
	public static string PurchaseReturn => "PurchaseReturn";
	public static string PurchaseReturnDetail => "PurchaseReturnDetail";
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
	public static string LoadTableDataByCode => "Load_TableData_By_Code";
	public static string LoadTableDataByTransactionNo => "Load_TableData_By_TransactionNo";
	public static string LoadLastTableDataByFinancialYear => "Load_LastTableData_By_FinancialYear";
	public static string LoadLastTableDataByLocationFinancialYear => "Load_LastTableData_By_Location_FinancialYear";
	public static string LoadCurrentDateTime => "Load_CurrentDateTime";
	public static string LoadSettingsByKey => "Load_Settings_By_Key";

	public static string LoadUserByPasscode => "Load_User_By_Passcode";
	public static string LoadCustomerByNumber => "Load_Customer_By_Number";

	public static string LoadFinancialYearByDateTime => "Load_FinancialYear_By_DateTime";

	public static string LoadRawMaterialByRawMaterialCategory => "Load_RawMaterial_By_RawMaterialCategory";

	public static string LoadRawMaterialByPartyPurchaseDateTime => "Load_RawMaterial_By_Party_PurchaseDateTime";
	public static string LoadPurchaseDetailByPurchase => "Load_PurchaseDetail_By_Purchase";
	public static string LoadPurchaseOverviewByDate => "Load_PurchaseOverview_By_Date";
	public static string LoadPurchaseItemOverviewByDate => "Load_Purchase_Item_Overview_By_Date";
	public static string LoadPurchaseReturnDetailByPurchaseReturn => "Load_PurchaseReturnDetail_By_PurchaseReturn";
	public static string LoadPurchaseReturnOverviewByDate => "Load_PurchaseReturn_Overview_By_Date";
	public static string LoadPurchaseReturnItemOverviewByDate => "Load_PurchaseReturn_Item_Overview_By_Date";

	public static string LoadKitchenIssueDetailByKitchenIssue => "Load_KitchenIssueDetail_By_KitchenIssue";
	public static string LoadKitchenIssueOverviewByDate => "Load_KitchenIssueOverview_By_Date";
	public static string LoadKitchenIssueItemOverviewByDate => "Load_KitchenIssue_Item_Overview_By_Date";
	public static string LoadKitchenProductionDetailByKitchenProduction => "Load_KitchenProductionDetail_By_KitchenProduction";
	public static string LoadKitchenProductionOverviewByDate => "Load_KitchenProductionOverview_By_Date";
	public static string LoadKitchenProductionItemOverviewByDate => "Load_KitchenProduction_Item_Overview_By_Date";

	public static string LoadRawMaterialStockSummaryByDate => "Load_RawMaterialStockSummary_By_Date";
	public static string LoadRawMaterialStockDetailsByDate => "Load_RawMaterialStockDetails_By_Date";
	public static string LoadProductStockSummaryByDateLocationId => "Load_ProductStockSummary_By_Date_LocationId";
	public static string LoadProductStockDetailsByDateLocationId => "Load_ProductStockDetails_By_Date_LocationId";

	public static string LoadRecipeByProduct => "Load_Recipe_By_Product";
	public static string LoadRecipeDetailByRecipe => "Load_RecipeDetail_By_Recipe";

	public static string LoadProductByProductCategory => "Load_Product_By_ProductCategory";
	public static string LoadProductRateByProduct => "Load_ProductRate_By_Product";
	public static string LoadProductByLocation => "Load_Product_By_Location";

	public static string LoadSaleReturnDetailBySaleReturn => "Load_SaleReturnDetail_By_SaleReturn";
	public static string LoadSaleReturnOverviewByDate => "Load_SaleReturn_Overview_By_Date";
	public static string LoadSaleReturnItemOverviewByDate => "Load_SaleReturn_Item_Overview_By_Date";

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

	public static string LoadSaleDetailsByDateLocationId => "Load_SaleDetails_By_Date_LocationId";
	public static string LoadOrderDetailsByDateLocationId => "Load_OrderDetails_By_Date_LocationId";
	public static string LoadProductDetailsByDateLocationId => "Load_ProductDetails_By_Date_LocationId";

	public static string LoadLedgerByLocation => "Load_Ledger_By_Location";

	public static string LoadLastAccountingByFinancialYearVoucher => "Load_LastAccounting_By_FinancialYear_Voucher";
	public static string LoadAccountingDetailsByAccounting => "Load_AccountingDetails_By_Accounting";
	public static string LoadAccountingByTransactionNo => "Load_Accounting_By_TransactionNo";

	public static string LoadAccountingDetailsByDate => "Load_AccountingDetails_By_Date";
	public static string LoadAccountingOverviewByAccountingId => "Load_AccountingOverview_By_AccountingId";

	public static string LoadLedgerOverviewByAccountingId => "Load_LedgerOverview_By_AccountingId";
	public static string LoadLedgerDetailsByDateLedger => "Load_LedgerDetails_By_Date_Ledger";
	public static string LoadTrialBalanceByDate => "Load_TrialBalance_By_Date";

	public static string InsertUser => "Insert_User";
	public static string ResetSettings => "Reset_Settings";
	public static string UpdateSettings => "Update_Settings";
	public static string InsertLocation => "Insert_Location";
	public static string InsertCustomer => "Insert_Customer";
	public static string InsertStateUT => "Insert_StateUT";
	public static string InsertCompany => "Insert_Company";
	public static string InsertTax => "Insert_Tax";

	public static string InsertProductCategory => "Insert_ProductCategory";
	public static string InsertProduct => "Insert_Product";
	public static string InsertProductLocation => "Insert_ProductLocation";

	public static string InsertRawMaterialCategory => "Insert_RawMaterialCategory";
	public static string InsertRawMaterial => "Insert_RawMaterial";

	public static string InsertPurchase => "Insert_Purchase";
	public static string InsertPurchaseDetail => "Insert_PurchaseDetail";
	public static string InsertPurchaseReturn => "Insert_PurchaseReturn";
	public static string InsertPurchaseReturnDetail => "Insert_PurchaseReturnDetail";

	public static string InsertKitchen => "Insert_Kitchen";
	public static string InsertKitchenIssue => "Insert_KitchenIssue";
	public static string InsertKitchenIssueDetail => "Insert_KitchenIssueDetail";
	public static string InsertKitchenProduction => "Insert_KitchenProduction";
	public static string InsertKitchenProductionDetail => "Insert_KitchenProductionDetail";

	public static string InsertProductStock => "Insert_ProductStock";
	public static string InsertRawMaterialStock => "Insert_RawMaterialStock";

	public static string InsertRecipe => "Insert_Recipe";
	public static string InsertRecipeDetail => "Insert_RecipeDetail";

	public static string InsertSale => "Insert_Sale";
	public static string InsertSaleDetail => "Insert_SaleDetail";
	public static string InsertSaleReturn => "Insert_SaleReturn";
	public static string InsertSaleReturnDetail => "Insert_SaleReturnDetail";

	public static string InsertOrder => "Insert_Order";
	public static string InsertOrderDetail => "Insert_OrderDetail";

	public static string InsertLedger => "Insert_Ledger";
	public static string InsertGroup => "Insert_Group";
	public static string InsertAccountType => "Insert_AccountType";
	public static string InsertVoucher => "Insert_Voucher";
	public static string InsertFinancialYear => "Insert_FinancialYear";

	public static string InsertAccounting => "Insert_Accounting";
	public static string InsertAccountingDetails => "Insert_AccountingDetails";

	public static string DeleteProductStockById => "Delete_ProductStock_By_Id";
	public static string DeleteProductStockByTypeTransactionIdLocationId => "Delete_ProductStock_By_Type_TransactionId_LocationId";
	public static string DeleteRawMaterialStockById => "Delete_RawMaterialStock_By_Id";
	public static string DeleteRawMaterialStockByTypeTransactionId => "Delete_RawMaterialStock_By_Type_TransactionId";
}

public static class ViewNames
{
	public static string PurchaseOverview => "Purchase_Overview";
	public static string PurchaseReturnOverview => "PurchaseReturn_Overview";
	public static string PurchaseItemOverview => "Purchase_Item_Overview";
	public static string PurchaseReturnItemOverview => "PurchaseReturn_Item_Overview";

	public static string KitchenIssueOverview => "KitchenIssue_Overview";
	public static string KitchenProductionOverview => "KitchenProduction_Overview";
	public static string KitchenIssueItemOverview => "KitchenIssue_Item_Overview";
	public static string KitchenProductionItemOverview => "KitchenProduction_Item_Overview";

	public static string RawMaterialStockDetails => "RawMaterialStockDetails";
	public static string ProductStockDetails => "ProductStockDetails";

	public static string SaleOverview => "Sale_Overview";
	public static string SaleItemOverview => "Sale_Item_Overview";
	public static string SaleReturnOverview => "SaleReturn_Overview";
	public static string SaleReturnItemOverview => "SaleReturn_Item_Overview";

	public static string OrderOverview => "Order_Overview";
	public static string ProductOverview => "Product_Overview";
	public static string AccountingOverview => "Accounting_Overview";
	public static string LedgerOverview => "Ledger_Overview";
	public static string ProductLocationOverview => "ProductLocation_Overview";
}