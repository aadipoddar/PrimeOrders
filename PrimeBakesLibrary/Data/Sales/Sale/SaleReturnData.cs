using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.Exporting.Sales.Sale;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Stock;
using PrimeBakesLibrary.Models.Sales.Sale;

namespace PrimeBakesLibrary.Data.Sales.Sale;

public static class SaleReturnData
{
	public static async Task<int> InsertSaleReturn(SaleReturnModel saleReturn) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertSaleReturn, saleReturn)).FirstOrDefault();

	public static async Task<int> InsertSaleReturnDetail(SaleReturnDetailModel saleReturnDetail) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertSaleReturnDetail, saleReturnDetail)).FirstOrDefault();

	public static async Task<List<SaleReturnDetailModel>> LoadSaleReturnDetailBySaleReturn(int SaleReturnId) =>
		await SqlDataAccess.LoadData<SaleReturnDetailModel, dynamic>(StoredProcedureNames.LoadSaleReturnDetailBySaleReturn, new { SaleReturnId });

	public static async Task<List<SaleReturnOverviewModel>> LoadSaleReturnOverviewByDate(DateTime StartDate, DateTime EndDate, bool OnlyActive = true) =>
		await SqlDataAccess.LoadData<SaleReturnOverviewModel, dynamic>(StoredProcedureNames.LoadSaleReturnOverviewByDate, new { StartDate, EndDate, OnlyActive });

	public static async Task<List<SaleReturnItemOverviewModel>> LoadSaleReturnItemOverviewByDate(DateTime StartDate, DateTime EndDate) =>
		await SqlDataAccess.LoadData<SaleReturnItemOverviewModel, dynamic>(StoredProcedureNames.LoadSaleReturnItemOverviewByDate, new { StartDate, EndDate });

	public static async Task<(MemoryStream pdfStream, string fileName)> GenerateAndDownloadInvoice(int saleReturnId)
	{
		try
		{
			// Load saved sale return details
			var savedSaleReturn = await CommonData.LoadTableDataById<SaleReturnModel>(TableNames.SaleReturn, saleReturnId) ??
				throw new InvalidOperationException("Saved sale return transaction not found.");

			// Load sale return details from database
			var saleReturnDetails = await LoadSaleReturnDetailBySaleReturn(saleReturnId);
			if (saleReturnDetails is null || saleReturnDetails.Count == 0)
				throw new InvalidOperationException("No sale return details found for invoice generation.");

			// Load company, location, and party
			var company = await CommonData.LoadTableDataById<CompanyModel>(TableNames.Company, savedSaleReturn.CompanyId);
			var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, savedSaleReturn.LocationId);

			// Try to load party (party can be null for cash sales)
			LedgerModel party = null;
			if (savedSaleReturn.PartyId.HasValue && savedSaleReturn.PartyId.Value > 0)
				party = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, savedSaleReturn.PartyId.Value);

			// Try to load customer (customer can be null)
			CustomerModel customer = null;
			if (savedSaleReturn.CustomerId.HasValue && savedSaleReturn.CustomerId.Value > 0)
				customer = await CommonData.LoadTableDataById<CustomerModel>(TableNames.Customer, savedSaleReturn.CustomerId.Value);

			if (company is null)
				throw new InvalidOperationException("Invoice generation skipped - company not found.");

			// Generate invoice PDF
			var pdfStream = await SaleReturnInvoicePDFExport.ExportSaleReturnInvoice(
				savedSaleReturn,
				saleReturnDetails,
				company,
				party,
				customer,
				null, // logo path - uses default
				"SALE RETURN INVOICE",
				location?.Name // outlet
			);

			// Generate file name
			var currentDateTime = await CommonData.LoadCurrentDateTime();
			string fileName = $"SALE_RETURN_INVOICE_{savedSaleReturn.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}.pdf";
			return (pdfStream, fileName);
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException($"Invoice generation failed: {ex.Message}", ex);
		}
	}

	public static async Task DeleteSaleReturn(int saleReturnId)
	{
		var saleReturn = await CommonData.LoadTableDataById<SaleReturnModel>(TableNames.SaleReturn, saleReturnId);
		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, saleReturn.FinancialYearId);
		if (financialYear is null || financialYear.Locked || financialYear.Status == false)
			throw new InvalidOperationException("Cannot delete sale return transaction as the financial year is locked.");

		if (saleReturn is not null)
		{
			saleReturn.Status = false;
			await InsertSaleReturn(saleReturn);

			await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(StockType.SaleReturn.ToString(), saleReturn.Id, saleReturn.LocationId);
			await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(StockType.SaleReturn.ToString(), saleReturn.Id);

			if (saleReturn.PartyId is not null || saleReturn.PartyId > 0)
			{
				var party = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, saleReturn.PartyId.Value);
				if (party.LocationId.HasValue && party.LocationId.Value > 0)
					await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(StockType.PurchaseReturn.ToString(), saleReturn.Id, party.LocationId.Value);
			}

			var existingAccounting = await AccountingData.LoadAccountingByTransactionNo(saleReturn.TransactionNo);
			if (existingAccounting is not null && existingAccounting.Id > 0)
			{
				existingAccounting.Status = false;
				await AccountingData.InsertAccounting(existingAccounting);
			}
		}
	}

	public static async Task RecoverSaleReturnTransaction(SaleReturnModel saleReturn)
	{
		var saleDetails = await LoadSaleReturnDetailBySaleReturn(saleReturn.Id);
		List<SaleReturnItemCartModel> saleItemCarts = [];

		foreach (var item in saleDetails)
			saleItemCarts.Add(new()
			{
				ItemId = item.SaleReturnId,
				ItemName = "",
				Quantity = item.Quantity,
				Rate = item.Rate,
				BaseTotal = item.BaseTotal,
				DiscountPercent = item.DiscountPercent,
				DiscountAmount = item.DiscountAmount,
				AfterDiscount = item.AfterDiscount,
				CGSTPercent = item.CGSTPercent,
				CGSTAmount = item.CGSTAmount,
				SGSTPercent = item.SGSTPercent,
				SGSTAmount = item.SGSTAmount,
				IGSTPercent = item.IGSTPercent,
				IGSTAmount = item.IGSTAmount,
				InclusiveTax = item.InclusiveTax,
				TotalTaxAmount = item.TotalTaxAmount,
				Total = item.Total,
				NetRate = item.NetRate,
				Remarks = item.Remarks
			});

		await SaveSaleReturnTransaction(saleReturn, saleItemCarts);
	}

	public static async Task<int> SaveSaleReturnTransaction(SaleReturnModel saleReturn, List<SaleReturnItemCartModel> saleReturnDetails)
	{
		bool update = saleReturn.Id > 0;

		if (update)
		{
			var existingSaleReturn = await CommonData.LoadTableDataById<SaleReturnModel>(TableNames.SaleReturn, saleReturn.Id);
			var updateFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, existingSaleReturn.FinancialYearId);
			if (updateFinancialYear is null || updateFinancialYear.Locked || updateFinancialYear.Status == false)
				throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");

			saleReturn.TransactionNo = existingSaleReturn.TransactionNo;
		}
		else
			saleReturn.TransactionNo = await GenerateCodes.GenerateSaleReturnTransactionNo(saleReturn);

		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, saleReturn.FinancialYearId);
		if (financialYear is null || financialYear.Locked || financialYear.Status == false)
			throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");

		saleReturn.Id = await InsertSaleReturn(saleReturn);
		await SaveSaleReturnDetail(saleReturn, saleReturnDetails, update);
		await SaveProductStock(saleReturn, saleReturnDetails, update);
		await SaveRawMaterialStockByRecipe(saleReturn, saleReturnDetails, update);
		await SaveAccounting(saleReturn, update);

		return saleReturn.Id;
	}

	private static async Task SaveSaleReturnDetail(SaleReturnModel saleReturn, List<SaleReturnItemCartModel> saleReturnDetails, bool update)
	{
		if (update)
		{
			var existingSaleDetails = await LoadSaleReturnDetailBySaleReturn(saleReturn.Id);
			foreach (var item in existingSaleDetails)
			{
				item.Status = false;
				await InsertSaleReturnDetail(item);
			}
		}

		foreach (var item in saleReturnDetails)
			await InsertSaleReturnDetail(new()
			{
				Id = 0,
				SaleReturnId = saleReturn.Id,
				ProductId = item.ItemId,
				Quantity = item.Quantity,
				Rate = item.Rate,
				BaseTotal = item.BaseTotal,
				DiscountPercent = item.DiscountPercent,
				DiscountAmount = item.DiscountAmount,
				AfterDiscount = item.AfterDiscount,
				CGSTPercent = item.CGSTPercent,
				CGSTAmount = item.CGSTAmount,
				SGSTPercent = item.SGSTPercent,
				SGSTAmount = item.SGSTAmount,
				IGSTPercent = item.IGSTPercent,
				IGSTAmount = item.IGSTAmount,
				TotalTaxAmount = item.TotalTaxAmount,
				InclusiveTax = item.InclusiveTax,
				NetRate = item.NetRate,
				Total = item.Total,
				Remarks = item.Remarks,
				Status = true
			});
	}

	private static async Task SaveProductStock(SaleReturnModel saleReturn, List<SaleReturnItemCartModel> cart, bool update)
	{
		if (update)
		{
			await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(StockType.SaleReturn.ToString(), saleReturn.Id, saleReturn.LocationId);

			if (saleReturn.PartyId is not null || saleReturn.PartyId > 0)
			{
				var party = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, saleReturn.PartyId.Value);
				if (party.LocationId.HasValue && party.LocationId.Value > 0)
					await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(StockType.PurchaseReturn.ToString(), saleReturn.Id, party.LocationId.Value);
			}
		}

		// Location Stock Update
		foreach (var item in cart)
			await ProductStockData.InsertProductStock(new()
			{
				Id = 0,
				ProductId = item.ItemId,
				Quantity = item.Quantity,
				NetRate = item.NetRate,
				TransactionId = saleReturn.Id,
				Type = StockType.SaleReturn.ToString(),
				TransactionNo = saleReturn.TransactionNo,
				TransactionDate = DateOnly.FromDateTime(saleReturn.TransactionDateTime),
				LocationId = saleReturn.LocationId
			});

		// Party Location Stock Update
		if (saleReturn.PartyId is not null || saleReturn.PartyId > 0)
		{
			var party = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, saleReturn.PartyId.Value);
			if (party.LocationId.HasValue && party.LocationId.Value > 0)
				foreach (var item in cart)
					await ProductStockData.InsertProductStock(new()
					{
						Id = 0,
						ProductId = item.ItemId,
						Quantity = -item.Quantity,
						NetRate = item.NetRate,
						TransactionId = saleReturn.Id,
						Type = StockType.PurchaseReturn.ToString(),
						TransactionNo = saleReturn.TransactionNo,
						TransactionDate = DateOnly.FromDateTime(saleReturn.TransactionDateTime),
						LocationId = party.LocationId.Value
					});
		}
	}

	private static async Task SaveRawMaterialStockByRecipe(SaleReturnModel saleReturn, List<SaleReturnItemCartModel> cart, bool update)
	{
		if (update)
			await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(StockType.SaleReturn.ToString(), saleReturn.Id);

		if (saleReturn.LocationId != 1)
			return;

		foreach (var product in cart)
		{
			var recipe = await RecipeData.LoadRecipeByProduct(product.ItemId);
			var recipeItems = recipe is null ? [] : await RecipeData.LoadRecipeDetailByRecipe(recipe.Id);

			foreach (var recipeItem in recipeItems)
				await RawMaterialStockData.InsertRawMaterialStock(new()
				{
					Id = 0,
					RawMaterialId = recipeItem.RawMaterialId,
					Quantity = recipeItem.Quantity * product.Quantity,
					NetRate = product.NetRate / recipeItem.Quantity,
					TransactionId = saleReturn.Id,
					TransactionNo = saleReturn.TransactionNo,
					Type = StockType.SaleReturn.ToString(),
					TransactionDate = DateOnly.FromDateTime(saleReturn.TransactionDateTime)
				});
		}
	}

	private static async Task SaveAccounting(SaleReturnModel saleReturn, bool update)
	{
		if (saleReturn.LocationId != 1)
			return;

		if (update)
		{
			var existingAccounting = await AccountingData.LoadAccountingByTransactionNo(saleReturn.TransactionNo);
			if (existingAccounting is not null && existingAccounting.Id > 0)
			{
				existingAccounting.Status = false;
				await AccountingData.InsertAccounting(existingAccounting);
			}
		}

		var saleReturnOverview = await CommonData.LoadTableDataById<SaleReturnOverviewModel>(ViewNames.SaleReturnOverview, saleReturn.Id);
		if (saleReturnOverview.TotalAmount <= 0 && saleReturnOverview.TotalTaxAmount <= 0)
			return;

		int accountingId = await AccountingData.InsertAccounting(new()
		{
			Id = 0,
			TransactionNo = saleReturn.TransactionNo,
			AccountingDate = DateOnly.FromDateTime(saleReturn.TransactionDateTime),
			FinancialYearId = (await FinancialYearData.LoadFinancialYearByDateTime(saleReturn.TransactionDateTime)).Id,
			VoucherId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.SaleReturnVoucherId)).Value),
			Remarks = saleReturn.Remarks,
			UserId = saleReturn.CreatedBy,
			GeneratedModule = GeneratedModules.SaleReturn.ToString(),
			CreatedAt = await CommonData.LoadCurrentDateTime(),
			Status = true
		});

		await SaveAccountingDetails(saleReturnOverview, accountingId);
	}

	private static async Task SaveAccountingDetails(SaleReturnOverviewModel saleReturnOverview, int accountingId)
	{
		// Party Account Posting (Credit)
		if (saleReturnOverview.TotalAmount > 0)
			await AccountingData.InsertAccountingDetails(new()
			{
				Id = 0,
				AccountingId = accountingId,
				LedgerId = saleReturnOverview.Credit > 0 ? saleReturnOverview.PartyId.Value : int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.CashLedgerId)).Value),
				ReferenceId = saleReturnOverview.Id,
				ReferenceType = ReferenceTypes.SaleReturn.ToString(),
				Debit = null,
				Credit = saleReturnOverview.TotalAmount,
				Remarks = $"Cash / Party Account Posting For Sale Return Bill {saleReturnOverview.TransactionNo}",
				Status = true
			});

		// Sale Return Account Posting (Debit)
		if (saleReturnOverview.TotalAmount - saleReturnOverview.TotalTaxAmount > 0)
			await AccountingData.InsertAccountingDetails(new()
			{
				Id = 0,
				AccountingId = accountingId,
				LedgerId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.SaleLedgerId)).Value),
				ReferenceId = saleReturnOverview.Id,
				ReferenceType = ReferenceTypes.SaleReturn.ToString(),
				Debit = saleReturnOverview.TotalAmount - saleReturnOverview.TotalTaxAmount,
				Credit = null,
				Remarks = $"Sale Return Account Posting For Sale Return Bill {saleReturnOverview.TransactionNo}",
				Status = true
			});

		// GST Account Posting (Debit)
		if (saleReturnOverview.TotalTaxAmount > 0)
			await AccountingData.InsertAccountingDetails(new()
			{
				Id = 0,
				AccountingId = accountingId,
				LedgerId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.GSTLedgerId)).Value),
				ReferenceId = saleReturnOverview.Id,
				ReferenceType = ReferenceTypes.SaleReturn.ToString(),
				Debit = saleReturnOverview.TotalTaxAmount,
				Credit = null,
				Remarks = $"GST Account Posting For Sale Return Bill {saleReturnOverview.TransactionNo}",
				Status = true
			});
	}
}
