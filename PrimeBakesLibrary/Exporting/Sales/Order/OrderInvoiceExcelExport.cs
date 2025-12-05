using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sales.Order;
using PrimeBakesLibrary.Models.Sales.Product;

namespace PrimeBakesLibrary.Exporting.Sales.Order;

/// <summary>
/// Convert Order data to Invoice Excel format
/// </summary>
public static class OrderInvoiceExcelExport
{
	/// <summary>
	/// Export Order as a professional invoice Excel (automatically loads item names)
	/// </summary>
	/// <param name="orderHeader">Order header data</param>
	/// <param name="orderDetails">Order detail line items</param>
	/// <param name="company">Company information</param>
	/// <param name="location">Location information</param>
	/// <param name="saleTransactionNo">Optional: Sale transaction number if order converted to sale</param>
	/// <param name="saleDateTime">Optional: Sale date/time if order converted to sale</param>
	/// <param name="logoPath">Optional: Path to company logo</param>
	/// <param name="invoiceType">Type of document (ORDER INVOICE, ORDER CONFIRMATION, etc.)</param>
	/// <param name="outlet">Optional: Outlet/Location name</param>
	/// <returns>MemoryStream containing the Excel file</returns>
	public static async Task<MemoryStream> ExportOrderInvoice(
		OrderModel orderHeader,
		List<OrderDetailModel> orderDetails,
		CompanyModel company,
		LocationModel location,
		string saleTransactionNo = null,
		DateTime? saleDateTime = null,
		string logoPath = null,
		string invoiceType = "ORDER CONFIRMATION",
		string outlet = null)
	{
		// Load all items to get names
		var allItems = await CommonData.LoadTableData<ProductModel>(TableNames.Product);

		// Map line items with actual item names (Order has no financial details, just quantity)
		var lineItems = orderDetails.Select(detail =>
		{
			var item = allItems.FirstOrDefault(i => i.Id == detail.ProductId);
			string itemName = item?.Name ?? $"Item #{detail.ProductId}";

			return new ExcelInvoiceExportUtil.InvoiceLineItem
			{
				ItemId = detail.ProductId,
				ItemName = itemName,
				Quantity = detail.Quantity,
				Rate = 0, // Orders don't have rates
				DiscountPercent = 0,
				AfterDiscount = 0,
				CGSTPercent = 0,
				SGSTPercent = 0,
				IGSTPercent = 0,
				TotalTaxAmount = 0,
				Total = 0
			};
		}).ToList();

		// Map invoice header data (Order has no financial details)
		var invoiceData = new ExcelInvoiceExportUtil.InvoiceData
		{
			TransactionNo = orderHeader.TransactionNo,
			TransactionDateTime = orderHeader.TransactionDateTime,
			OrderTransactionNo = saleTransactionNo, // Show sale if converted
			OrderDateTime = saleDateTime,
			ItemsTotalAmount = 0,
			OtherChargesAmount = 0,
			OtherChargesPercent = 0,
			CashDiscountAmount = 0,
			CashDiscountPercent = 0,
			RoundOffAmount = 0,
			TotalAmount = 0,
			Cash = 0,
			Card = 0,
			UPI = 0,
			Credit = 0,
			Remarks = orderHeader.Remarks,
			Status = orderHeader.Status
		};

		// Generate invoice Excel with generic models
		// For orders, we pass location as billTo since orders are typically for specific locations
		var billTo = new LedgerModel
		{
			Id = location.Id,
			Name = location.Name
		};

		return await ExcelInvoiceExportUtil.ExportInvoiceToExcel(
			invoiceData,
			lineItems,
			company,
			billTo,
			logoPath,
			invoiceType,
			outlet,
			null // No customer for orders
		);
	}

	/// <summary>
	/// Export Order with item names already provided
	/// </summary>
	public static async Task<MemoryStream> ExportOrderInvoiceWithItems(
		OrderModel orderHeader,
		List<OrderItemCartModel> orderItems,
		CompanyModel company,
		LocationModel location,
		string saleTransactionNo = null,
		DateTime? saleDateTime = null,
		string logoPath = null,
		string invoiceType = "ORDER CONFIRMATION",
		string outlet = null)
	{
		// Map line items (items already have names)
		var lineItems = orderItems.Select(item => new ExcelInvoiceExportUtil.InvoiceLineItem
		{
			ItemId = item.ItemId,
			ItemName = item.ItemName,
			Quantity = item.Quantity,
			Rate = 0,
			DiscountPercent = 0,
			AfterDiscount = 0,
			CGSTPercent = 0,
			SGSTPercent = 0,
			IGSTPercent = 0,
			TotalTaxAmount = 0,
			Total = 0
		}).ToList();

		// Map invoice header data
		var invoiceData = new ExcelInvoiceExportUtil.InvoiceData
		{
			TransactionNo = orderHeader.TransactionNo,
			TransactionDateTime = orderHeader.TransactionDateTime,
			OrderTransactionNo = saleTransactionNo,
			OrderDateTime = saleDateTime,
			ItemsTotalAmount = 0,
			OtherChargesAmount = 0,
			OtherChargesPercent = 0,
			CashDiscountAmount = 0,
			CashDiscountPercent = 0,
			RoundOffAmount = 0,
			TotalAmount = 0,
			Cash = 0,
			Card = 0,
			UPI = 0,
			Credit = 0,
			Remarks = orderHeader.Remarks,
			Status = orderHeader.Status
		};

		// Generate invoice Excel
		var billTo = new LedgerModel
		{
			Id = location.Id,
			Name = location.Name
		};

		return await ExcelInvoiceExportUtil.ExportInvoiceToExcel(
			invoiceData,
			lineItems,
			company,
			billTo,
			logoPath,
			invoiceType,
			outlet,
			null
		);
	}
}
