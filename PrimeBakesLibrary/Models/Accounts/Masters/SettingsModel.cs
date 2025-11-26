namespace PrimeBakesLibrary.Models.Accounts.Masters;

public class SettingsModel
{
    public string Key { get; set; }
    public string Value { get; set; }
    public string Description { get; set; }
}

public static class SettingsKeys
{
    public static string RawMaterialCodePrefix => "RawMaterialCodePrefix";
    public static string FinishedProductCodePrefix => "FinishedProductCodePrefix";
    public static string LedgerCodePrefix => "LedgerCodePrefix";

    public static string PurchaseTransactionPrefix => "PurchaseTransactionPrefix";
    public static string PurchaseReturnTransactionPrefix => "PurchaseReturnTransactionPrefix";
    public static string KitchenIssueTransactionPrefix => "KitchenIssueTransactionPrefix";
    public static string KitchenProductionTransactionPrefix => "KitchenProductionTransactionPrefix";
    public static string RawMaterialStockAdjustmentTransactionPrefix => "RawMaterialStockAdjustmentTransactionPrefix";
    public static string ProductStockAdjustmentTransactionPrefix => "ProductStockAdjustmentTransactionPrefix";

    public static string SaleTransactionPrefix => "SaleTransactionPrefix";
    public static string SaleReturnTransactionPrefix => "SaleReturnTransactionPrefix";
    public static string OrderTransactionPrefix => "OrderTransactionPrefix";
    public static string AccountingTransactionPrefix => "AccountingTransactionPrefix";

    public static string UpdateItemMasterRateOnPurchase => "UpdateRawMaterialMasterRateOnPurchase";
    public static string UpdateItemMasterUOMOnPurchase => "UpdateRawMaterialMasterUOMOnPurchase";

    public static string PrimaryCompanyLinkingId => "PrimaryCompanyLinkingId";

    public static string SaleVoucherId => "SaleVoucherId";
    public static string SaleReturnVoucherId => "SaleReturnVoucherId";
    public static string PurchaseVoucherId => "PurchaseVoucherId";
    public static string PurchaseReturnVoucherId => "PurchaseReturnVoucherId";
    public static string SaleLedgerId => "SaleLedgerId";
    public static string PurchaseLedgerId => "PurchaseLedgerId";
    public static string CashLedgerId => "CashLedgerId";
    public static string CashSalesLedgerId => "CashSalesLedgerId";
    public static string GSTLedgerId => "GSTLedgerId";
}