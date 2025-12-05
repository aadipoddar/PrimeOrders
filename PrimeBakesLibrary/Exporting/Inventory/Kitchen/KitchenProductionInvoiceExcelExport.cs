using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Sales.Product;

namespace PrimeBakesLibrary.Exporting.Inventory.Kitchen;

/// <summary>
/// Convert Kitchen Production data to Invoice Excel format
/// </summary>
public static class KitchenProductionInvoiceExcelExport
{
	/// <summary>
	/// Export Kitchen Production as a professional invoice Excel (automatically loads product names)
	/// </summary>
	/// <param name="kitchenProductionHeader">Kitchen Production header data</param>
	/// <param name="kitchenProductionDetails">Kitchen Production detail line items</param>
	/// <param name="company">Company information</param>
	/// <param name="kitchen">Kitchen/Location information</param>
	/// <param name="logoPath">Optional: Path to company logo</param>
	/// <param name="invoiceType">Type of document (KITCHEN PRODUCTION INVOICE, etc.)</param>
	/// <returns>MemoryStream containing the Excel file</returns>
	public static async Task<MemoryStream> ExportKitchenProductionInvoice(
		KitchenProductionModel kitchenProductionHeader,
		List<KitchenProductionDetailModel> kitchenProductionDetails,
		CompanyModel company,
		LocationModel kitchen,
		string logoPath = null,
		string invoiceType = "KITCHEN PRODUCTION INVOICE")
	{
		// Load all products to get names
		var allProducts = await CommonData.LoadTableData<ProductModel>(TableNames.Product);

		// Map line items with actual product names
		var cartItems = kitchenProductionDetails.Select(detail =>
		{
			var product = allProducts.FirstOrDefault(p => p.Id == detail.ProductId);
			string productName = product?.Name ?? $"Product #{detail.ProductId}";

			return new KitchenProductionProductCartModel
			{
				ProductId = detail.ProductId,
				ProductName = productName,
				Quantity = detail.Quantity,
				Rate = detail.Rate,
				Total = detail.Total,
				Remarks = detail.Remarks
			};
		}).ToList();

		// Use the simplified export method
		return await ExportKitchenProductionInvoiceWithItems(
			kitchenProductionHeader,
			cartItems,
			company,
			kitchen,
			logoPath,
			invoiceType
		);
	}

	/// <summary>
	/// Export Kitchen Production with product names already loaded
	/// Uses generic invoice utility with zero discount/tax for simplified format
	/// </summary>
	public static async Task<MemoryStream> ExportKitchenProductionInvoiceWithItems(
		KitchenProductionModel kitchenProductionHeader,
		List<KitchenProductionProductCartModel> kitchenProductionProducts,
		CompanyModel company,
		LocationModel kitchen,
		string logoPath = null,
		string invoiceType = "KITCHEN PRODUCTION INVOICE")
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

		// Map line items to generic invoice format (no discount/tax for kitchen production)
		var lineItems = kitchenProductionProducts.Select(product => new ExcelInvoiceExportUtil.InvoiceLineItem
		{
			ItemId = product.ProductId,
			ItemName = product.ProductName,
			Quantity = product.Quantity,
			UnitOfMeasurement = "",
			Rate = product.Rate,
			DiscountPercent = 0,
			AfterDiscount = product.Quantity * product.Rate,
			CGSTPercent = 0,
			SGSTPercent = 0,
			IGSTPercent = 0,
			TotalTaxAmount = 0,
			Total = product.Total
		}).ToList();

		// Map invoice header data
		var invoiceData = new ExcelInvoiceExportUtil.InvoiceData
		{
			TransactionNo = kitchenProductionHeader.TransactionNo,
			TransactionDateTime = kitchenProductionHeader.TransactionDateTime,
			ItemsTotalAmount = kitchenProductionHeader.TotalAmount,
			OtherChargesAmount = 0,
			OtherChargesPercent = 0,
			CashDiscountAmount = 0,
			CashDiscountPercent = 0,
			RoundOffAmount = 0,
			TotalAmount = kitchenProductionHeader.TotalAmount,
			Remarks = kitchenProductionHeader.Remarks,
			Status = kitchenProductionHeader.Status
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
