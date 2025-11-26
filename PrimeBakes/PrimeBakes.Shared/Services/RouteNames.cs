namespace PrimeBakes.Shared.Services;

internal static class PageRouteNames
{
    public static string Dashboard => "/";

    public static string InventoryDashboard => "/inventory";
    public static string Purchase => "/inventory/purchase";
    public static string PurchaseReturn => "/inventory/purchase-return";
    public static string KitchenIssue => "/inventory/kitchen-issue";
    public static string KitchenProduction => "/inventory/kitchen-production";
    public static string ProductStockAdjustment => "/inventory/product-stock-adjustment";
    public static string RawMaterialStockAdjustment => "/inventory/raw-material-stock-adjustment";
    public static string Recipe => "/inventory/recipe";

    public static string SalesDashboard => "/sales";
    public static string SaleReturn => "/sales/sale-return";
    public static string Sale => "/sales/sale";
    public static string Order => "/sales/order";
    public static string OrderMobile => "/sales/order/mobile";
    public static string OrderMobileCart => "/sales/order-cart/mobile";
    public static string OrderMobileConfirmation => "/sales/order-confirmation/mobile";

    public static string AccountsDashboard => "/accounts";
    public static string FinancialAccounting => "/accounts/financial-accounting";

    public static string ReportDashboard => "/report";
    public static string ReportPurchase => "/report/purchase";
    public static string ReportPurchaseReturn => "/report/purchase-return";
    public static string ReportPurchaseItem => "/report/purchase-item";
    public static string ReportPurchaseReturnItem => "/report/purchase-return-item";
    public static string ReportKitchenIssue => "/report/kitchen-issue";
    public static string ReportKitchenProduction => "/report/kitchen-production";
    public static string ReportKitchenIssueItem => "/report/kitchen-issue-item";
    public static string ReportKitchenProductionItem => "/report/kitchen-production-item";
    public static string ReportRawMaterialStock => "/report/raw-material-stock";
    public static string ReportProductStock => "/report/product-stock";

    public static string ReportSaleReturn => "/report/sale-return";
    public static string ReportSaleReturnItem => "/report/sale-return-item";
    public static string ReportSale => "/report/sale";
    public static string ReportSaleItem => "/report/sale-item";
    public static string ReportOrder => "/report/order";
    public static string ReportOrderItem => "/report/order-item";

    public static string ReportFinancialAccounting => "/report/financial-accounting";
    public static string ReportAccountingLedger => "/report/accounting-ledger";
    public static string ReportTrialBalance => "/report/trial-balance";

    public static string AdminDashboard => "/admin";
    public static string AdminLocation => "/admin/location";
    public static string AdminRawMaterial => "/admin/raw-material";
    public static string AdminRawMaterialCategory => "/admin/raw-material-category";
    public static string AdminKitchen => "/admin/kitchen";
    public static string AdminProduct => "/admin/product";
    public static string AdminProductCategory => "/admin/product-category";
    public static string AdminUser => "/admin/user";
    public static string AdminTax => "/admin/tax";
    public static string AdminCompany => "/admin/company";
    public static string AdminLedger => "/admin/ledger";
    public static string AdminVoucher => "/admin/voucher";
    public static string AdminGroup => "/admin/group";
    public static string AdminAccountType => "/admin/account-type";
    public static string AdminFinancialYear => "/admin/financial-year";
    public static string AdminState => "/admin/state";
}
