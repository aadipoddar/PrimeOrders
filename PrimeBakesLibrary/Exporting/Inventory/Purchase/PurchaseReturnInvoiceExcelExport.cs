using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Inventory.Purchase;

namespace PrimeBakesLibrary.Exporting.Inventory.Purchase;

/// <summary>
/// Convert Purchase Return data to Invoice Excel format
/// </summary>
public static class PurchaseReturnInvoiceExcelExport
{
	/// <summary>
	/// Export Purchase Return as a professional invoice Excel (automatically loads item names)
	/// </summary>
	/// <param name="purchaseReturnHeader">Purchase return header data</param>
	/// <param name="purchaseReturnDetails">Purchase return detail line items</param>
	/// <param name="company">Company information</param>
	/// <param name="party">Party/Supplier information</param>
	/// <param name="logoPath">Optional: Path to company logo</param>
	/// <param name="invoiceType">Type of document (PURCHASE RETURN, DEBIT NOTE, etc.)</param>
	/// <returns>MemoryStream containing the Excel file</returns>
	public static async Task<MemoryStream> ExportPurchaseReturnInvoice(
		PurchaseReturnModel purchaseReturnHeader,
		List<PurchaseReturnDetailModel> purchaseReturnDetails,
		CompanyModel company,
		LedgerModel party,
		string logoPath = null,
		string invoiceType = "PURCHASE RETURN")
	{
		// Load all items to get names
		var allItems = await CommonData.LoadTableData<RawMaterialModel>(TableNames.RawMaterial);

		// Map line items with actual item names
		var lineItems = purchaseReturnDetails.Select(detail =>
		{
			var item = allItems.FirstOrDefault(i => i.Id == detail.RawMaterialId);
			string itemName = item?.Name ?? $"Item #{detail.RawMaterialId}";

			return new ExcelInvoiceExportUtil.InvoiceLineItem
			{
				ItemId = detail.RawMaterialId,
				ItemName = itemName,
				Quantity = detail.Quantity,
				UnitOfMeasurement = detail.UnitOfMeasurement,
				Rate = detail.Rate,
				DiscountPercent = detail.DiscountPercent,
				AfterDiscount = detail.AfterDiscount,
				CGSTPercent = detail.InclusiveTax ? 0 : detail.CGSTPercent,
				SGSTPercent = detail.InclusiveTax ? 0 : detail.SGSTPercent,
				IGSTPercent = detail.InclusiveTax ? 0 : detail.IGSTPercent,
				TotalTaxAmount = detail.InclusiveTax ? 0 : detail.TotalTaxAmount,
				Total = detail.Total
			};
		}).ToList();

		// Map invoice header data
		var invoiceData = new ExcelInvoiceExportUtil.InvoiceData
		{
			TransactionNo = purchaseReturnHeader.TransactionNo,
			TransactionDateTime = purchaseReturnHeader.TransactionDateTime,
			ItemsTotalAmount = purchaseReturnHeader.TotalAfterTax,
			OtherChargesAmount = purchaseReturnHeader.OtherChargesAmount,
			OtherChargesPercent = purchaseReturnHeader.OtherChargesPercent,
			CashDiscountAmount = purchaseReturnHeader.CashDiscountAmount,
			CashDiscountPercent = purchaseReturnHeader.CashDiscountPercent,
			RoundOffAmount = purchaseReturnHeader.RoundOffAmount,
			TotalAmount = purchaseReturnHeader.TotalAmount,
			Remarks = purchaseReturnHeader.Remarks,
			Status = purchaseReturnHeader.Status
		};

		// Generate invoice Excel with generic models
		return await ExcelInvoiceExportUtil.ExportInvoiceToExcel(
			invoiceData,
			lineItems,
			company,
			party,
			logoPath,
			invoiceType
		);
	}
}
