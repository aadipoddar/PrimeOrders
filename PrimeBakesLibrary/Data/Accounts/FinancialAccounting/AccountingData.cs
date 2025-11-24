using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.Data.Sales.Order;
using PrimeBakesLibrary.Exporting.Sales.Sale;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Stock;
using PrimeBakesLibrary.Models.Sales.Order;
using PrimeBakesLibrary.Models.Sales.Sale;

namespace PrimeBakesLibrary.Data.Accounts.FinancialAccounting;

public static class AccountingData
{
	public static async Task<int> InsertAccounting(AccountingModel accounting) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertAccounting, accounting)).FirstOrDefault();

	public static async Task<int> InsertAccountingDetail(AccountingDetailModel accountingDetails) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertAccountingDetail, accountingDetails)).FirstOrDefault();

	public static async Task<List<AccountingDetailModel>> LoadAccountingDetailByAccounting(int AccountingId) =>
		await SqlDataAccess.LoadData<AccountingDetailModel, dynamic>(StoredProcedureNames.LoadAccountingDetailByAccounting, new { AccountingId });

	public static async Task<List<AccountingOverviewModel>> LoadAccountingOverviewByDate(DateTime StartDate, DateTime EndDate, bool OnlyActive = true) =>
		await SqlDataAccess.LoadData<AccountingOverviewModel, dynamic>(StoredProcedureNames.LoadAccountingOverviewByDate, new { StartDate, EndDate, OnlyActive });

	public static async Task<AccountingModel> LoadAccountingByVoucherReference(int VoucherId, int ReferenceId, string ReferenceNo) =>
		(await SqlDataAccess.LoadData<AccountingModel, dynamic>(StoredProcedureNames.LoadAccountingByVoucherReference, new { VoucherId, ReferenceId, ReferenceNo })).FirstOrDefault();

	public static async Task<(MemoryStream pdfStream, string fileName)> GenerateAndDownloadInvoice(int accountingId)
	{
		//try
		//{
		//	// Load saved sale details
		//	var savedSale = await CommonData.LoadTableDataById<SaleModel>(TableNames.Sale, saleId) ??
		//		throw new InvalidOperationException("Saved sale transaction not found.");

		//	// Load sale details from database
		//	var saleDetails = await LoadSaleDetailBySale(saleId);
		//	if (saleDetails is null || saleDetails.Count == 0)
		//		throw new InvalidOperationException("No sale details found for invoice generation.");

		//	// Load company, location, and party
		//	var company = await CommonData.LoadTableDataById<CompanyModel>(TableNames.Company, savedSale.CompanyId);
		//	var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, savedSale.LocationId);

		//	// Try to load party (party can be null for cash sales)
		//	LedgerModel party = null;
		//	if (savedSale.PartyId.HasValue && savedSale.PartyId.Value > 0)
		//		party = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, savedSale.PartyId.Value);

		//	// Try to load customer (customer can be null)
		//	CustomerModel customer = null;
		//	if (savedSale.CustomerId.HasValue && savedSale.CustomerId.Value > 0)
		//		customer = await CommonData.LoadTableDataById<CustomerModel>(TableNames.Customer, savedSale.CustomerId.Value);

		//	// Try to load order information if OrderId is present
		//	OrderModel order = null;
		//	if (savedSale.OrderId.HasValue && savedSale.OrderId.Value > 0)
		//		order = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, savedSale.OrderId.Value);

		//	if (company is null)
		//		throw new InvalidOperationException("Invoice generation skipped - company not found.");

		//	// Generate invoice PDF
		//	var pdfStream = await SaleInvoicePDFExport.ExportSaleInvoice(
		//		savedSale,
		//		saleDetails,
		//		company,
		//		party,
		//		customer,
		//		order?.TransactionNo,
		//		order?.TransactionDateTime,
		//		null, // logo path - uses default
		//		"SALE INVOICE",
		//		location?.Name // outlet
		//	);

		//	// Generate file name
		//	var currentDateTime = await CommonData.LoadCurrentDateTime();
		//	string fileName = $"SALE_INVOICE_{savedSale.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}.pdf";
		//	return (pdfStream, fileName);
		//}
		//catch (Exception ex)
		//{
		//	throw new InvalidOperationException($"Invoice generation failed: {ex.Message}", ex);
		//}

		return (null, string.Empty); // Placeholder return
	}

	public static async Task DeleteAccounting(int accountingId)
	{
		var accounting = await CommonData.LoadTableDataById<AccountingModel>(TableNames.Accounting, accountingId);
		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, accounting.FinancialYearId);
		if (financialYear is null || financialYear.Locked || financialYear.Status == false)
			throw new InvalidOperationException("Cannot delete accounting transaction as the financial year is locked.");

		accounting.Status = false;
		await InsertAccounting(accounting);
	}

	public static async Task RecoverAccountingTransaction(AccountingModel accounting)
	{
		var accountingDetails = await LoadAccountingDetailByAccounting(accounting.Id);
		List<AccountingItemCartModel> accountingItemCarts = [];

		foreach (var item in accountingDetails)
			accountingItemCarts.Add(new()
			{
				LedgerName = string.Empty,
				LedgerId = item.LedgerId,
				ReferenceType = item.ReferenceType,
				Credit = item.Credit,
				Debit = item.Debit,
				ReferenceId = item.ReferenceId,
				ReferenceNo = item.ReferenceId.HasValue ? item.ReferenceId.Value.ToString() : null,
				Remarks = item.Remarks
			});

		await SaveAccountingTransaction(accounting, accountingItemCarts);
	}

	public static async Task<int> SaveAccountingTransaction(AccountingModel accounting, List<AccountingItemCartModel> accountingDetails)
	{
		bool update = accounting.Id > 0;

		if (update)
		{
			var existingAccounting = await CommonData.LoadTableDataById<AccountingModel>(TableNames.Accounting, accounting.Id);
			var updateFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, existingAccounting.FinancialYearId);
			if (updateFinancialYear is null || updateFinancialYear.Locked || updateFinancialYear.Status == false)
				throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");

			accounting.TransactionNo = existingAccounting.TransactionNo;
		}
		else
			accounting.TransactionNo = await GenerateCodes.GenerateAccountingTransactionNo(accounting);

		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, accounting.FinancialYearId);
		if (financialYear is null || financialYear.Locked || financialYear.Status == false)
			throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");

		accounting.Id = await InsertAccounting(accounting);
		await SaveAccountingDetail(accounting, accountingDetails, update);

		return accounting.Id;
	}

	private static async Task SaveAccountingDetail(AccountingModel accounting, List<AccountingItemCartModel> accountingDetails, bool update)
	{
		if (update)
		{
			var existingAccountingDetails = await LoadAccountingDetailByAccounting(accounting.Id);
			foreach (var item in existingAccountingDetails)
			{
				item.Status = false;
				await InsertAccountingDetail(item);
			}
		}

		foreach (var item in accountingDetails)
			await InsertAccountingDetail(new()
			{
				Id = 0,
				AccountingId = accounting.Id,
				LedgerId = item.LedgerId,
				Credit = item.Credit,
				Debit = item.Debit,
				ReferenceType = item.ReferenceType,
				ReferenceId = item.ReferenceId,
				ReferenceNo = item.ReferenceNo,
				Remarks = item.Remarks,
				Status = true
			});
	}
}
