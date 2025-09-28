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
	public static string PurchaseVoucherId => "PurchaseVoucherId";
	public static string SaleReturnVoucherId => "SaleReturnVoucherId";
	public static string SaleLedgerId => "SaleLedgerId";
	public static string PurchaseLedgerId => "PurchaseLedgerId";
	public static string CashLedgerId => "CashLedgerId";
	public static string GSTLedgerId => "GSTLedgerId";
}