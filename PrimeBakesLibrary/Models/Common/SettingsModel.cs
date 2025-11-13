namespace PrimeBakesLibrary.Models.Common;

public class SettingsModel
{
	public string Key { get; set; }
	public string Value { get; set; }
	public string Description { get; set; }
}

public static class SettingsKeys
{
	public static string SalesVoucherId => "SalesVoucherId";
	public static string SaleReturnVoucherId => "SaleReturnVoucherId";
	public static string PurchaseVoucherId => "PurchaseVoucherId";
	public static string PurchaseReturnVoucherId => "PurchaseReturnVoucherId";
	public static string SaleLedgerId => "SaleLedgerId";
	public static string PurchaseLedgerId => "PurchaseLedgerId";
	public static string CashLedgerId => "CashLedgerId";
	public static string GSTLedgerId => "GSTLedgerId";

	public static string PrimaryCompanyLinkingId => "PrimaryCompanyLinkingId";

	public static string LedgerCodePrefix => "LedgerCodePrefix";

	public static string PurchaseTransactionPrefix => "PurchaseTransactionPrefix";
	public static string PurchaseReturnTransactionPrefix => "PurchaseReturnTransactionPrefix";
	public static string KitchenIssueTransactionPrefix => "KitchenIssueTransactionPrefix";
	public static string KitchenProductionTransactionPrefix => "KitchenProductionTransactionPrefix";
	public static string RawMaterialStockAdjustmentTransactionPrefix => "RawMaterialStockAdjustmentTransactionPrefix";
	public static string ProductStockAdjustmentTransactionPrefix => "ProductStockAdjustmentTransactionPrefix";

	public static string UpdateItemMasterRateOnPurchase => "UpdateRawMaterialMasterRateOnPurchase";
	public static string UpdateItemMasterUOMOnPurchase => "UpdateRawMaterialMasterUOMOnPurchase";
}