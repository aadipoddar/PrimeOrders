using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Inventory.Kitchen;

namespace PrimeBakesLibrary.Exporting.Inventory.Kitchen;

/// <summary>
/// Convert Kitchen Issue data to Invoice Excel format
/// </summary>
public static class KitchenIssueInvoiceExcelExport
{
	/// <summary>
	/// Export Kitchen Issue as a professional invoice Excel (automatically loads item names)
	/// </summary>
	/// <param name="kitchenIssueHeader">Kitchen Issue header data</param>
	/// <param name="kitchenIssueDetails">Kitchen Issue detail line items</param>
	/// <param name="company">Company information</param>
	/// <param name="kitchen">Kitchen/Location information</param>
	/// <param name="logoPath">Optional: Path to company logo</param>
	/// <param name="invoiceType">Type of document (KITCHEN ISSUE INVOICE, etc.)</param>
	/// <returns>MemoryStream containing the Excel file</returns>
	public static async Task<MemoryStream> ExportKitchenIssueInvoice(
		KitchenIssueModel kitchenIssueHeader,
		List<KitchenIssueDetailModel> kitchenIssueDetails,
		CompanyModel company,
		LocationModel kitchen,
		string logoPath = null,
		string invoiceType = "KITCHEN ISSUE INVOICE")
	{
		// Load all raw materials to get names
		var allItems = await CommonData.LoadTableData<RawMaterialModel>(TableNames.RawMaterial);

		// Map line items with actual item names
		var cartItems = kitchenIssueDetails.Select(detail =>
		{
			var item = allItems.FirstOrDefault(i => i.Id == detail.RawMaterialId);
			string itemName = item?.Name ?? $"Item #{detail.RawMaterialId}";

			return new KitchenIssueItemCartModel
			{
				ItemId = detail.RawMaterialId,
				ItemName = itemName,
				Quantity = detail.Quantity,
				UnitOfMeasurement = detail.UnitOfMeasurement,
				Rate = detail.Rate,
				Total = detail.Total,
				Remarks = detail.Remarks
			};
		}).ToList();

		// Use the simplified export method
		return await ExportKitchenIssueInvoiceWithItems(
			kitchenIssueHeader,
			cartItems,
			company,
			kitchen,
			logoPath,
			invoiceType
		);
	}

	/// <summary>
	/// Export Kitchen Issue with item names already loaded
	/// Uses generic invoice utility with zero discount/tax for simplified format
	/// </summary>
	public static async Task<MemoryStream> ExportKitchenIssueInvoiceWithItems(
		KitchenIssueModel kitchenIssueHeader,
		List<KitchenIssueItemCartModel> kitchenIssueItems,
		CompanyModel company,
		LocationModel kitchen,
		string logoPath = null,
		string invoiceType = "KITCHEN ISSUE INVOICE")
	{
		// Convert LocationModel to LedgerModel format for generic utility
		var kitchenAsLedger = new LedgerModel
		{
			Name = kitchen.Name,
			Address = "",
			Phone = "",
			Email = "",
			GSTNo = ""
		};

		// Map line items to generic invoice format (no discount/tax for kitchen issues)
		var lineItems = kitchenIssueItems.Select(item => new ExcelInvoiceExportUtil.InvoiceLineItem
		{
			ItemId = item.ItemId,
			ItemName = item.ItemName,
			Quantity = item.Quantity,
			UnitOfMeasurement = item.UnitOfMeasurement,
			Rate = item.Rate,
			DiscountPercent = 0,
			AfterDiscount = item.Quantity * item.Rate,
			CGSTPercent = 0,
			SGSTPercent = 0,
			IGSTPercent = 0,
			TotalTaxAmount = 0,
			Total = item.Total
		}).ToList();

		// Map invoice header data
		var invoiceData = new ExcelInvoiceExportUtil.InvoiceData
		{
			TransactionNo = kitchenIssueHeader.TransactionNo,
			TransactionDateTime = kitchenIssueHeader.TransactionDateTime,
			ItemsTotalAmount = kitchenIssueHeader.TotalAmount,
			OtherChargesAmount = 0,
			OtherChargesPercent = 0,
			CashDiscountAmount = 0,
			CashDiscountPercent = 0,
			RoundOffAmount = 0,
			TotalAmount = kitchenIssueHeader.TotalAmount,
			Remarks = kitchenIssueHeader.Remarks,
			Status = kitchenIssueHeader.Status
		};

		// Generate invoice Excel using generic utility
		return await ExcelInvoiceExportUtil.ExportInvoiceToExcel(
			invoiceData,
			lineItems,
			company,
			kitchenAsLedger,
			logoPath,
			invoiceType
		);
	}
}
