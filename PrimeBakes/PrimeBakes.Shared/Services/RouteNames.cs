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

	public static string ReportPurchase => "/report/purchase";
	public static string ReportPurchaseReturn => "/report/purchase-return";
	public static string ReportKitchenIssue => "/report/kitchen-issue";
	public static string ReportKitchenProduction => "/report/kitchen-production";
	public static string ReportRawMaterialStock => "/report/raw-material-stock";
	public static string ReportProductStock => "/report/product-stock";

	public static string AdminLocation => "/admin/location";
	public static string AdminRawMaterial => "/admin/raw-material";
	public static string AdminKitchen => "/admin/kitchen";
	public static string AdminProduct => "/admin/product";
	public static string AdminCompany => "/admin/company";
	public static string AdminLedger => "/admin/ledger";
}
