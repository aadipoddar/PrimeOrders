using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Sales.Order;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sales.Order;
using PrimeBakesLibrary.Models.Sales.Sale;

namespace PrimeBakesLibrary.Data.Sales.Order;

public static class OrderData
{
	public static async Task<int> InsertOrder(OrderModel order) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertOrder, order)).FirstOrDefault();

	public static async Task<int> InsertOrderDetail(OrderDetailModel orderDetail) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertOrderDetail, orderDetail)).FirstOrDefault();

	public static async Task<List<OrderModel>> LoadOrderByLocationPending(int LocationId) =>
		await SqlDataAccess.LoadData<OrderModel, dynamic>(StoredProcedureNames.LoadOrderByLocationPending, new { LocationId });

	public static async Task<(MemoryStream pdfStream, string fileName)> GenerateAndDownloadInvoice(int orderId)
	{
		try
		{
			// Load saved order details
			var transaction = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, orderId) ??
				throw new InvalidOperationException("Transaction not found.");

			// Load order details from database
			var orderDetails = await CommonData.LoadTableDataByMasterId<OrderDetailModel>(TableNames.OrderDetail, orderId);
			if (orderDetails is null || orderDetails.Count == 0)
				throw new InvalidOperationException("No transaction details found for the transaction.");

			// Load company and location
			var company = await CommonData.LoadTableDataById<CompanyModel>(TableNames.Company, transaction.CompanyId);
			var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, transaction.LocationId);

			if (company is null)
				throw new InvalidOperationException("Invoice generation skipped - company not found.");
			if (location is null)
				throw new InvalidOperationException("Invoice generation skipped - location not found.");

			// Try to load sale information if order is converted to sale
			SaleModel sale = null;
			if (transaction.SaleId is > 0)
				sale = await CommonData.LoadTableDataById<SaleModel>(TableNames.Sale, transaction.SaleId.Value);

			// Generate invoice PDF
			var pdfStream = await OrderInvoicePDFExport.ExportOrderInvoice(
				transaction,
				orderDetails,
				company,
				location,
				sale?.TransactionNo,
				sale?.TransactionDateTime,
				null, // logo path - uses default
				"ORDER CONFIRMATION",
				location?.Name // outlet
			);

			// Generate file name
			var currentDateTime = await CommonData.LoadCurrentDateTime();
			string fileName = $"ORDER_INVOICE_{transaction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}.pdf";
			return (pdfStream, fileName);
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException($"Invoice generation failed: {ex.Message}", ex);
		}
	}

	public static async Task<(MemoryStream excelStream, string fileName)> GenerateAndDownloadExcelInvoice(int orderId)
	{
		try
		{
			// Load saved order details
			var transaction = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, orderId) ??
				throw new InvalidOperationException("Transaction not found.");

			// Load order details from database
			var orderDetails = await CommonData.LoadTableDataByMasterId<OrderDetailModel>(TableNames.OrderDetail, orderId);
			if (orderDetails is null || orderDetails.Count == 0)
				throw new InvalidOperationException("No transaction details found for the transaction.");

			// Load company and location
			var company = await CommonData.LoadTableDataById<CompanyModel>(TableNames.Company, transaction.CompanyId);
			var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, transaction.LocationId);

			if (company is null)
				throw new InvalidOperationException("Invoice generation skipped - company not found.");
			if (location is null)
				throw new InvalidOperationException("Invoice generation skipped - location not found.");

			// Try to load sale information if order is converted to sale
			SaleModel sale = null;
			if (transaction.SaleId is > 0)
				sale = await CommonData.LoadTableDataById<SaleModel>(TableNames.Sale, transaction.SaleId.Value);

			// Generate invoice Excel
			var excelStream = await OrderInvoiceExcelExport.ExportOrderInvoice(
				transaction,
				orderDetails,
				company,
				location,
				sale?.TransactionNo,
				sale?.TransactionDateTime,
				null, // logoPath
				"ORDER CONFIRMATION", // invoiceType
				location?.Name // outlet
			);

			// Generate file name
			var currentDateTime = await CommonData.LoadCurrentDateTime();
			string fileName = $"ORDER_INVOICE_{transaction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}.xlsx";
			return (excelStream, fileName);
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException($"Excel invoice generation failed: {ex.Message}", ex);
		}
	}

	public static async Task DeleteOrder(int orderId)
	{
		var order = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, orderId);
		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, order.FinancialYearId);
		if (financialYear is null || financialYear.Locked || !financialYear.Status)
			throw new InvalidOperationException("Cannot delete transaction as the financial year is locked.");

		if (order.SaleId is not null && order.SaleId > 0)
			throw new InvalidOperationException("Cannot delete order as it is already converted to a sale.");

		order.Status = false;
		await InsertOrder(order);
	}

	public static async Task RecoverOrderTransaction(OrderModel order)
	{
		var transactionDetails = await CommonData.LoadTableDataByMasterId<OrderDetailModel>(TableNames.OrderDetail, order.Id);
		List<OrderItemCartModel> orderItemCarts = [];

		foreach (var item in transactionDetails)
			orderItemCarts.Add(new()
			{
				ItemId = item.ProductId,
				ItemName = "",
				Quantity = item.Quantity,
				Remarks = item.Remarks
			});

		await SaveOrderTransaction(order, orderItemCarts);
	}

	public static async Task<int> SaveOrderTransaction(OrderModel order, List<OrderItemCartModel> orderDetails)
	{
		bool update = order.Id > 0;

		if (update)
		{
			var existingOrder = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, order.Id);
			var updateFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, existingOrder.FinancialYearId);
			if (updateFinancialYear is null || updateFinancialYear.Locked || !updateFinancialYear.Status)
				throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");

			if (existingOrder.SaleId is not null && existingOrder.SaleId > 0)
				throw new InvalidOperationException("Cannot update order as it is already converted to a sale.");

			order.TransactionNo = existingOrder.TransactionNo;
		}
		else
			order.TransactionNo = await GenerateCodes.GenerateOrderTransactionNo(order);

		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, order.FinancialYearId);
		if (financialYear is null || financialYear.Locked || !financialYear.Status)
			throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");

		order.Id = await InsertOrder(order);
		await SaveOrderDetail(order, orderDetails, update);

		return order.Id;
	}

	private static async Task SaveOrderDetail(OrderModel order, List<OrderItemCartModel> orderDetails, bool update)
	{
		if (update)
		{
			var existingOrderDetails = await CommonData.LoadTableDataByMasterId<OrderDetailModel>(TableNames.OrderDetail, order.Id);
			foreach (var item in existingOrderDetails)
			{
				item.Status = false;
				await InsertOrderDetail(item);
			}
		}

		foreach (var item in orderDetails)
			await InsertOrderDetail(new()
			{
				Id = 0,
				MasterId = order.Id,
				ProductId = item.ItemId,
				Quantity = item.Quantity,
				Remarks = item.Remarks,
				Status = true
			});
	}
}