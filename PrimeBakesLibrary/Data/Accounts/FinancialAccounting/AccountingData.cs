using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Data.Accounts.FinancialAccounting;

public static class AccountingData
{
	public static async Task<int> InsertAccounting(AccountingModel accounting) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertAccounting, accounting)).FirstOrDefault();

	public static async Task InsertAccountingDetails(AccountingDetailsModel accountingDetails) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertAccountingDetails, accountingDetails);

	public static async Task<AccountingModel> LoadLastAccountingByFinancialYearVoucher(int FinancialYearId, int VoucherId) =>
		(await SqlDataAccess.LoadData<AccountingModel, dynamic>(StoredProcedureNames.LoadLastAccountingByFinancialYearVoucher, new { FinancialYearId, VoucherId })).FirstOrDefault();

	public static async Task<List<AccountingDetailsModel>> LoadAccountingDetailsByAccounting(int AccountingId) =>
		await SqlDataAccess.LoadData<AccountingDetailsModel, dynamic>(StoredProcedureNames.LoadAccountingDetailsByAccounting, new { AccountingId });

	public static async Task<AccountingModel> LoadAccountingByTransactionNo(string TransactionNo) =>
		(await SqlDataAccess.LoadData<AccountingModel, dynamic>(StoredProcedureNames.LoadAccountingByTransactionNo, new { TransactionNo })).FirstOrDefault();

	public static async Task<List<AccountingOverviewModel>> LoadAccountingDetailsByDate(DateTime FromDate, DateTime ToDate) =>
		await SqlDataAccess.LoadData<AccountingOverviewModel, dynamic>(StoredProcedureNames.LoadAccountingDetailsByDate, new { FromDate, ToDate });

	public static async Task<AccountingOverviewModel> LoadAccountingOverviewByAccountingId(int AccountingId) =>
		(await SqlDataAccess.LoadData<AccountingOverviewModel, dynamic>(StoredProcedureNames.LoadAccountingOverviewByAccountingId, new { AccountingId })).FirstOrDefault();

	public static async Task<List<LedgerOverviewModel>> LoadLedgerOverviewByAccountingId(int AccountingId) =>
		await SqlDataAccess.LoadData<LedgerOverviewModel, dynamic>(StoredProcedureNames.LoadLedgerOverviewByAccountingId, new { AccountingId });

	public static async Task<List<TrialBalanceModel>> LoadTrialBalanceByDate(DateTime FromDate, DateTime ToDate) =>
		await SqlDataAccess.LoadData<TrialBalanceModel, dynamic>(StoredProcedureNames.LoadTrialBalanceByDate, new { FromDate, ToDate });

	public static async Task<int> SaveAccountingTransaction(AccountingModel accounting, List<AccountingCartModel> cart)
	{
		bool update = accounting.Id > 0;

		accounting.Status = true;
		accounting.GeneratedModule = GeneratedModules.FinancialAccounting.ToString();
		accounting.FinancialYearId = (await FinancialYearData.LoadFinancialYearByDate(accounting.AccountingDate)).Id;

		if (update)
			accounting.TransactionNo = (await CommonData.LoadTableDataById<AccountingModel>(TableNames.Accounting, accounting.Id)).TransactionNo;
		else
			accounting.TransactionNo = await GenerateCodes.GenerateAccountingTransactionNo(accounting);

		accounting.Id = await InsertAccounting(accounting);
		await SaveAccountingDetails(accounting, cart, update);

		return accounting.Id;
	}

	private static async Task SaveAccountingDetails(AccountingModel accounting, List<AccountingCartModel> cart, bool update)
	{
		if (update)
		{
			var existingEntry = await LoadAccountingDetailsByAccounting(accounting.Id);
			foreach (var detail in existingEntry)
			{
				detail.Status = false;
				await InsertAccountingDetails(detail);
			}
		}

		foreach (var item in cart)
			await InsertAccountingDetails(new()
			{
				Id = 0,
				AccountingId = accounting.Id,
				LedgerId = item.Id,
				ReferenceId = item.ReferenceId,
				ReferenceType = item.ReferenceType,
				Debit = item.Debit,
				Credit = item.Credit,
				Remarks = item.Remarks ?? string.Empty,
				Status = true
			});
	}
}
