using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.Exporting.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Inventory.Stock;
using PrimeBakesLibrary.Models.Product;

namespace PrimeBakesLibrary.Data.Inventory.Kitchen;

public static class KitchenProductionData
{
	public static async Task<int> InsertKitchenProduction(KitchenProductionModel kitchenProduction) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertKitchenProduction, kitchenProduction)).FirstOrDefault();

	public static async Task<int> InsertKitchenProductionDetail(KitchenProductionDetailModel kitchenProductionDetail) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertKitchenProductionDetail, kitchenProductionDetail)).FirstOrDefault();

	public static async Task<List<KitchenProductionDetailModel>> LoadKitchenProductionDetailByKitchenProduction(int KitchenProductionId) =>
		await SqlDataAccess.LoadData<KitchenProductionDetailModel, dynamic>(StoredProcedureNames.LoadKitchenProductionDetailByKitchenProduction, new { KitchenProductionId });

	public static async Task<List<KitchenProductionOverviewModel>> LoadKitchenProductionOverviewByDate(DateTime StartDate, DateTime EndDate, bool OnlyActive = true) =>
		await SqlDataAccess.LoadData<KitchenProductionOverviewModel, dynamic>(StoredProcedureNames.LoadKitchenProductionOverviewByDate, new { StartDate, EndDate, OnlyActive });

	public static async Task<List<KitchenProductionItemOverviewModel>> LoadKitchenProductionItemOverviewByDate(DateTime StartDate, DateTime EndDate) =>
		await SqlDataAccess.LoadData<KitchenProductionItemOverviewModel, dynamic>(StoredProcedureNames.LoadKitchenProductionItemOverviewByDate, new { StartDate, EndDate });

	public static async Task<(MemoryStream pdfStream, string fileName)> GenerateAndDownloadInvoice(int kitchenProductionId)
	{
		try
		{
			// Load saved kitchen production details
			var savedKitchenProduction = await CommonData.LoadTableDataById<KitchenProductionModel>(TableNames.KitchenProduction, kitchenProductionId) ??
				throw new InvalidOperationException("Kitchen production transaction not found.");

			// Load kitchen production details from database
			var kitchenProductionDetails = await LoadKitchenProductionDetailByKitchenProduction(savedKitchenProduction.Id);
			if (kitchenProductionDetails is null || kitchenProductionDetails.Count == 0)
				throw new InvalidOperationException("No kitchen production details found for the transaction.");

			// Load company and kitchen
			var company = await CommonData.LoadTableDataById<CompanyModel>(TableNames.Company, savedKitchenProduction.CompanyId);
			var kitchen = await CommonData.LoadTableDataById<LocationModel>(TableNames.Kitchen, savedKitchenProduction.KitchenId);
			if (company is null || kitchen is null)
				throw new InvalidOperationException("Company or kitchen details not found.");

			// Convert kitchen production details to cart items with product names
			var products = await CommonData.LoadTableData<ProductModel>(TableNames.Product);
			var cartItems = new List<KitchenProductionProductCartModel>();
			foreach (var detail in kitchenProductionDetails)
			{
				var product = products.FirstOrDefault(p => p.Id == detail.ProductId);
				cartItems.Add(new KitchenProductionProductCartModel
				{
					ProductId = detail.ProductId,
					ProductName = product?.Name ?? "Unknown Product",
					Quantity = detail.Quantity,
					Rate = detail.Rate,
					Total = detail.Total,
					Remarks = detail.Remarks
				});
			}

			// Generate invoice PDF
			var pdfStream = await Task.Run(() =>
				KitchenProductionInvoicePDFExport.ExportKitchenProductionInvoiceWithItems(
					savedKitchenProduction,
					cartItems,
					company,
					kitchen,
					null, // logo path - uses default
					"KITCHEN PRODUCTION INVOICE"
				)
			);

			// Generate file name
			var currentDateTime = await CommonData.LoadCurrentDateTime();
			string fileName = $"KITCHEN_PRODUCTION_INVOICE_{savedKitchenProduction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}.pdf";
			return (pdfStream, fileName);
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException("Failed to generate and download invoice.", ex);
		}
	}

	public static async Task DeleteKitchenProduction(int kitchenProductionId)
	{
		var kitchenProduction = await CommonData.LoadTableDataById<KitchenProductionModel>(TableNames.KitchenProduction, kitchenProductionId);
		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, kitchenProduction.FinancialYearId);
		if (financialYear is null || financialYear.Locked || financialYear.Status == false)
			throw new InvalidOperationException("Cannot delete kitchen production transaction as the financial year is locked.");

		if (kitchenProduction is not null)
		{
			kitchenProduction.Status = false;
			await InsertKitchenProduction(kitchenProduction);
			await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(StockType.KitchenProduction.ToString(), kitchenProduction.Id, 1);
		}
	}

	public static async Task RecoverKitchenProductionTransaction(KitchenProductionModel kitchenProduction)
	{
		var kitchenProductionDetails = await LoadKitchenProductionDetailByKitchenProduction(kitchenProduction.Id);
		List<KitchenProductionProductCartModel> kitchenProductionProductCarts = [];

		foreach (var item in kitchenProductionDetails)
			kitchenProductionProductCarts.Add(new()
			{
				ProductId = item.ProductId,
				ProductName = "",
				Quantity = item.Quantity,
				Rate = item.Rate,
				Total = item.Total,
				Remarks = item.Remarks
			});

		await SaveKitchenProductionTransaction(kitchenProduction, kitchenProductionProductCarts);
	}

	public static async Task<int> SaveKitchenProductionTransaction(KitchenProductionModel kitchenProduction, List<KitchenProductionProductCartModel> kitchenProductionDetails)
	{
		bool update = kitchenProduction.Id > 0;

		if (update)
		{
			var existingKitchenProduction = await CommonData.LoadTableDataById<KitchenProductionModel>(TableNames.KitchenProduction, kitchenProduction.Id);
			var updateFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, existingKitchenProduction.FinancialYearId);
			if (updateFinancialYear is null || updateFinancialYear.Locked || updateFinancialYear.Status == false)
				throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");
		}

		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, kitchenProduction.FinancialYearId);
		if (financialYear is null || financialYear.Locked || financialYear.Status == false)
			throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");

		kitchenProduction.Id = await InsertKitchenProduction(kitchenProduction);
		await SaveKitchenProductionDetail(kitchenProduction, kitchenProductionDetails, update);
		await SaveProductStock(kitchenProduction, kitchenProductionDetails, update);
		// await SendNotification.SendKitchenProductionNotificationMainLocationAdminInventory(kitchenProduction.Id);

		return kitchenProduction.Id;
	}

	private static async Task SaveKitchenProductionDetail(KitchenProductionModel kitchenProduction, List<KitchenProductionProductCartModel> kitchenProductionDetails, bool update)
	{
		if (update)
		{
			var existingKitchenProductionDetails = await LoadKitchenProductionDetailByKitchenProduction(kitchenProduction.Id);
			foreach (var item in existingKitchenProductionDetails)
			{
				item.Status = false;
				await InsertKitchenProductionDetail(item);
			}
		}

		foreach (var item in kitchenProductionDetails)
			await InsertKitchenProductionDetail(new()
			{
				Id = 0,
				KitchenProductionId = kitchenProduction.Id,
				ProductId = item.ProductId,
				Quantity = item.Quantity,
				Rate = item.Rate,
				Total = item.Total,
				Remarks = item.Remarks,
				Status = true
			});
	}

	private static async Task SaveProductStock(KitchenProductionModel kitchenProduction, List<KitchenProductionProductCartModel> cart, bool update)
	{
		if (update)
			await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(StockType.KitchenProduction.ToString(), kitchenProduction.Id, 1);

		foreach (var item in cart)
			await ProductStockData.InsertProductStock(new()
			{
				Id = 0,
				ProductId = item.ProductId,
				Quantity = item.Quantity,
				NetRate = null,
				Type = StockType.KitchenProduction.ToString(),
				TransactionId = kitchenProduction.Id,
				TransactionNo = kitchenProduction.TransactionNo,
				TransactionDate = DateOnly.FromDateTime(kitchenProduction.TransactionDateTime),
				LocationId = 1, // Main Location
			});
	}
}
