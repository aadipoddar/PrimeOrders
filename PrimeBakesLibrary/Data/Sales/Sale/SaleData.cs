using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.Data.Sales.Order;
using PrimeBakesLibrary.Exporting.Sales.Sale;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Stock;
using PrimeBakesLibrary.Models.Sales.Order;
using PrimeBakesLibrary.Models.Sales.Sale;

namespace PrimeBakesLibrary.Data.Sales.Sale;

public static class SaleData
{
	public static async Task<int> InsertSale(SaleModel sale) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertSale, sale)).FirstOrDefault();

	public static async Task<int> InsertSaleDetail(SaleDetailModel saleDetail) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertSaleDetail, saleDetail)).FirstOrDefault();

	public static async Task<(MemoryStream pdfStream, string fileName)> GenerateAndDownloadInvoice(int saleId)
	{
		try
		{
			// Load saved sale details
			var transaction = await CommonData.LoadTableDataById<SaleModel>(TableNames.Sale, saleId) ??
				throw new InvalidOperationException("Transaction not found.");

			// Load sale details from database
			var transactionDetails = await CommonData.LoadTableDataByMasterId<SaleDetailModel>(TableNames.SaleDetail, saleId);
			if (transactionDetails is null || transactionDetails.Count == 0)
				throw new InvalidOperationException("No transaction details found for the transaction.");

			// Load company, location, and party
			var company = await CommonData.LoadTableDataById<CompanyModel>(TableNames.Company, transaction.CompanyId);
			var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, transaction.LocationId);

			// Try to load party (party can be null for cash sales)
			LedgerModel party = null;
			if (transaction.PartyId.HasValue && transaction.PartyId.Value > 0)
				party = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, transaction.PartyId.Value);

			// Try to load customer (customer can be null)
			CustomerModel customer = null;
			if (transaction.CustomerId.HasValue && transaction.CustomerId.Value > 0)
				customer = await CommonData.LoadTableDataById<CustomerModel>(TableNames.Customer, transaction.CustomerId.Value);

			// Try to load order information if OrderId is present
			OrderModel order = null;
			if (transaction.OrderId.HasValue && transaction.OrderId.Value > 0)
				order = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, transaction.OrderId.Value);

			if (company is null)
				throw new InvalidOperationException("Company information is missing.");

			// Generate invoice PDF
			var pdfStream = await SaleInvoicePDFExport.ExportSaleInvoice(
				transaction,
				transactionDetails,
				company,
				party,
				customer,
				order?.TransactionNo,
				order?.TransactionDateTime,
				null, // logo path - uses default
				"SALE INVOICE",
				location?.Name // outlet
			);

			// Generate file name
			var currentDateTime = await CommonData.LoadCurrentDateTime();
			string fileName = $"SALE_INVOICE_{transaction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}.pdf";
			return (pdfStream, fileName);
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException("Failed to generate and download invoice.", ex);
		}
	}

	public static async Task<(MemoryStream excelStream, string fileName)> GenerateAndDownloadExcelInvoice(int saleId)
	{
		try
		{
			// Load saved sale details
			var transaction = await CommonData.LoadTableDataById<SaleModel>(TableNames.Sale, saleId) ??
				throw new InvalidOperationException("Transaction not found.");

			// Load sale details from database
			var transactionDetails = await CommonData.LoadTableDataByMasterId<SaleDetailModel>(TableNames.SaleDetail, saleId);
			if (transactionDetails is null || transactionDetails.Count == 0)
				throw new InvalidOperationException("No transaction details found for the transaction.");

			// Load company, location, and party
			var company = await CommonData.LoadTableDataById<CompanyModel>(TableNames.Company, transaction.CompanyId);
			var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, transaction.LocationId);

			// Try to load party (party can be null for cash sales)
			LedgerModel party = null;
			if (transaction.PartyId.HasValue && transaction.PartyId.Value > 0)
				party = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, transaction.PartyId.Value);

			// Try to load customer (customer can be null)
			CustomerModel customer = null;
			if (transaction.CustomerId.HasValue && transaction.CustomerId.Value > 0)
				customer = await CommonData.LoadTableDataById<CustomerModel>(TableNames.Customer, transaction.CustomerId.Value);

			// Try to load order information if OrderId is present
			OrderModel order = null;
			if (transaction.OrderId.HasValue && transaction.OrderId.Value > 0)
				order = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, transaction.OrderId.Value);

			if (company is null)
				throw new InvalidOperationException("Company information is missing.");

			// Generate invoice Excel
			var excelStream = await SaleInvoiceExcelExport.ExportSaleInvoice(
				transaction,
				transactionDetails,
				company,
				party,
				customer,
				order?.TransactionNo,
				order?.TransactionDateTime,
				null, // logo path - uses default
				"SALE INVOICE",
				location?.Name // outlet
			);

			// Generate file name
			var currentDateTime = await CommonData.LoadCurrentDateTime();
			string fileName = $"SALE_INVOICE_{transaction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}.xlsx";
			return (excelStream, fileName);
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException("Failed to generate and download Excel invoice.", ex);
		}
	}

	public static async Task DeleteSale(int saleId)
	{
		var sale = await CommonData.LoadTableDataById<SaleModel>(TableNames.Sale, saleId);
		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, sale.FinancialYearId);
		if (financialYear is null || financialYear.Locked || financialYear.Status == false)
			throw new InvalidOperationException("Cannot delete transaction as the financial year is locked.");

		if (sale.OrderId is not null && sale.OrderId > 0)
			await UnlinkOrderFromSale(sale.Id);

		sale.OrderId = null;
		sale.Status = false;
		await InsertSale(sale);

		await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(StockType.Sale.ToString(), sale.Id, sale.LocationId);
		await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(StockType.Sale.ToString(), sale.Id);

		if (sale.PartyId is not null || sale.PartyId > 0)
		{
			var party = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, sale.PartyId.Value);
			if (party.LocationId.HasValue && party.LocationId.Value > 0)
				await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(StockType.Purchase.ToString(), sale.Id, party.LocationId.Value);
		}

		var saleVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleVoucherId);
		var existingAccounting = await AccountingData.LoadAccountingByVoucherReference(int.Parse(saleVoucher.Value), sale.Id, sale.TransactionNo);
		if (existingAccounting is not null && existingAccounting.Id > 0)
		{
			existingAccounting.Status = false;
			await AccountingData.InsertAccounting(existingAccounting);
		}
	}

	public static async Task RecoverSaleTransaction(SaleModel sale)
	{
		var transactionDetails = await CommonData.LoadTableDataByMasterId<SaleDetailModel>(TableNames.SaleDetail, sale.Id);
		List<SaleItemCartModel> transactionItemCarts = [];

		foreach (var item in transactionDetails)
			transactionItemCarts.Add(new()
			{
				ItemId = item.ProductId,
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

		await SaveSaleTransaction(sale, transactionItemCarts);
	}

	public static async Task UnlinkOrderFromSale(int saleId)
	{
		var sale = await CommonData.LoadTableDataById<SaleModel>(TableNames.Sale, saleId);

		if (sale.OrderId is null || sale.OrderId <= 0)
			return;

		var order = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, sale.OrderId.Value);
		if (order is not null && order.Id > 0)
		{
			order.SaleId = null;
			await OrderData.InsertOrder(order);
		}
	}

	public static async Task<int> SaveSaleTransaction(SaleModel sale, List<SaleItemCartModel> saleDetails)
	{
		bool update = sale.Id > 0;

		if (update)
		{
			var existingSale = await CommonData.LoadTableDataById<SaleModel>(TableNames.Sale, sale.Id);
			var updateFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, existingSale.FinancialYearId);
			if (updateFinancialYear is null || updateFinancialYear.Locked || updateFinancialYear.Status == false)
				throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");

			sale.TransactionNo = existingSale.TransactionNo;
		}
		else
			sale.TransactionNo = await GenerateCodes.GenerateSaleTransactionNo(sale);

		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, sale.FinancialYearId);
		if (financialYear is null || financialYear.Locked || financialYear.Status == false)
			throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");

		sale.Id = await InsertSale(sale);
		await SaveSaleDetail(sale, saleDetails, update);
		await SaveProductStock(sale, saleDetails, update);
		await SaveRawMaterialStockByRecipe(sale, saleDetails, update);
		await UpdateOrder(sale, update);
		await SaveAccounting(sale, update);

		return sale.Id;
	}

	private static async Task SaveSaleDetail(SaleModel sale, List<SaleItemCartModel> saleDetails, bool update)
	{
		if (update)
		{
			var existingSaleDetails = await CommonData.LoadTableDataByMasterId<SaleDetailModel>(TableNames.SaleDetail, sale.Id);
			foreach (var item in existingSaleDetails)
			{
				item.Status = false;
				await InsertSaleDetail(item);
			}
		}

		foreach (var item in saleDetails)
			await InsertSaleDetail(new()
			{
				Id = 0,
				MasterId = sale.Id,
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

	private static async Task SaveProductStock(SaleModel sale, List<SaleItemCartModel> cart, bool update)
	{
		if (update)
		{
			await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(StockType.Sale.ToString(), sale.Id, sale.LocationId);

			if (sale.PartyId is not null || sale.PartyId > 0)
			{
				var party = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, sale.PartyId.Value);
				if (party.LocationId.HasValue && party.LocationId.Value > 0)
					await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(StockType.Purchase.ToString(), sale.Id, party.LocationId.Value);
			}
		}

		// Location Stock Update
		foreach (var item in cart)
			await ProductStockData.InsertProductStock(new()
			{
				Id = 0,
				ProductId = item.ItemId,
				Quantity = -item.Quantity,
				NetRate = item.NetRate,
				TransactionId = sale.Id,
				Type = StockType.Sale.ToString(),
				TransactionNo = sale.TransactionNo,
				TransactionDate = DateOnly.FromDateTime(sale.TransactionDateTime),
				LocationId = sale.LocationId
			});

		// Party Location Stock Update
		if (sale.PartyId is not null || sale.PartyId > 0)
		{
			var party = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, sale.PartyId.Value);
			if (party.LocationId.HasValue && party.LocationId.Value > 0)
				foreach (var item in cart)
					await ProductStockData.InsertProductStock(new()
					{
						Id = 0,
						ProductId = item.ItemId,
						Quantity = item.Quantity,
						NetRate = item.NetRate,
						TransactionId = sale.Id,
						Type = StockType.Purchase.ToString(),
						TransactionNo = sale.TransactionNo,
						TransactionDate = DateOnly.FromDateTime(sale.TransactionDateTime),
						LocationId = party.LocationId.Value
					});
		}
	}

	private static async Task SaveRawMaterialStockByRecipe(SaleModel sale, List<SaleItemCartModel> cart, bool update)
	{
		if (update)
			await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(StockType.Sale.ToString(), sale.Id);

		if (sale.LocationId != 1)
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
					Quantity = -recipeItem.Quantity * product.Quantity,
					NetRate = product.NetRate / recipeItem.Quantity,
					TransactionId = sale.Id,
					TransactionNo = sale.TransactionNo,
					Type = StockType.Sale.ToString(),
					TransactionDate = DateOnly.FromDateTime(sale.TransactionDateTime)
				});
		}
	}

	private static async Task UpdateOrder(SaleModel sale, bool update)
	{
		if (update)
			await UnlinkOrderFromSale(sale.Id);

		if (sale.OrderId is null)
			return;

		var order = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, sale.OrderId.Value);
		if (order is not null && order.Status)
		{
			order.SaleId = sale.Id;
			await OrderData.InsertOrder(order);
		}
	}

	private static async Task SaveAccounting(SaleModel sale, bool update)
	{
		if (update)
		{
			var saleVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleVoucherId);
			var existingAccounting = await AccountingData.LoadAccountingByVoucherReference(int.Parse(saleVoucher.Value), sale.Id, sale.TransactionNo);
			if (existingAccounting is not null && existingAccounting.Id > 0)
			{
				existingAccounting.Status = false;
				await AccountingData.InsertAccounting(existingAccounting);
			}
		}

		if (sale.LocationId != 1)
			return;

		var saleOverview = await CommonData.LoadTableDataById<SaleOverviewModel>(ViewNames.SaleOverview, sale.Id);
		if (saleOverview is null)
			return;

		if (saleOverview.TotalAmount == 0)
			return;

		var accountingCart = new List<AccountingItemCartModel>();

		if (saleOverview.Cash + saleOverview.UPI + saleOverview.Card > 0)
		{
			var cashLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.CashSalesLedgerId);
			accountingCart.Add(new()
			{
				ReferenceId = saleOverview.Id,
				ReferenceType = ReferenceTypes.Sale.ToString(),
				ReferenceNo = saleOverview.TransactionNo,
				LedgerId = int.Parse(cashLedger.Value),
				Debit = saleOverview.Cash + saleOverview.UPI + saleOverview.Card,
				Credit = null,
				Remarks = $"Cash Account Posting For Sale Bill {saleOverview.TransactionNo}",
			});
		}

		if (saleOverview.Credit > 0)
			accountingCart.Add(new()
			{
				ReferenceId = saleOverview.Id,
				ReferenceType = ReferenceTypes.Sale.ToString(),
				ReferenceNo = saleOverview.TransactionNo,
				LedgerId = saleOverview.PartyId.Value,
				Debit = saleOverview.Credit,
				Credit = null,
				Remarks = $"Party Account Posting For Sale Bill {saleOverview.TransactionNo}",
			});

		if (saleOverview.TotalAmount - saleOverview.TotalExtraTaxAmount > 0)
		{
			var saleLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleLedgerId);
			accountingCart.Add(new()
			{
				ReferenceId = saleOverview.Id,
				ReferenceType = ReferenceTypes.Sale.ToString(),
				ReferenceNo = saleOverview.TransactionNo,
				LedgerId = int.Parse(saleLedger.Value),
				Debit = null,
				Credit = saleOverview.TotalAmount - saleOverview.TotalExtraTaxAmount,
				Remarks = $"Sale Account Posting For Sale Bill {saleOverview.TransactionNo}",
			});
		}

		if (saleOverview.TotalExtraTaxAmount > 0)
		{
			var gstLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.GSTLedgerId);
			accountingCart.Add(new()
			{
				ReferenceId = saleOverview.Id,
				ReferenceType = ReferenceTypes.Sale.ToString(),
				ReferenceNo = saleOverview.TransactionNo,
				LedgerId = int.Parse(gstLedger.Value),
				Debit = null,
				Credit = saleOverview.TotalExtraTaxAmount,
				Remarks = $"GST Account Posting For Sale Bill {saleOverview.TransactionNo}",
			});
		}

		var voucher = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleVoucherId);
		var accounting = new AccountingModel
		{
			Id = 0,
			TransactionNo = "",
			CompanyId = saleOverview.CompanyId,
			VoucherId = int.Parse(voucher.Value),
			ReferenceId = saleOverview.Id,
			ReferenceNo = saleOverview.TransactionNo,
			TransactionDateTime = saleOverview.TransactionDateTime,
			FinancialYearId = saleOverview.FinancialYearId,
			TotalDebitLedgers = accountingCart.Count(a => a.Debit.HasValue),
			TotalCreditLedgers = accountingCart.Count(a => a.Credit.HasValue),
			TotalDebitAmount = accountingCart.Sum(a => a.Debit ?? 0),
			TotalCreditAmount = accountingCart.Sum(a => a.Credit ?? 0),
			Remarks = saleOverview.Remarks,
			CreatedBy = saleOverview.CreatedBy,
			CreatedAt = saleOverview.CreatedAt,
			CreatedFromPlatform = saleOverview.CreatedFromPlatform,
			Status = true
		};

		await AccountingData.SaveAccountingTransaction(accounting, accountingCart);
	}
}
