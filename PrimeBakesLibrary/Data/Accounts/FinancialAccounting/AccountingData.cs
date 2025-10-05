using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;

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

	public static async Task<AccountingModel> LoadAccountingByReferenceNo(string ReferenceNo) =>
		(await SqlDataAccess.LoadData<AccountingModel, dynamic>(StoredProcedureNames.LoadAccountingByReferenceNo, new { ReferenceNo })).FirstOrDefault();

	public static async Task<List<AccountingOverviewModel>> LoadAccountingDetailsByDate(DateTime FromDate, DateTime ToDate) =>
		await SqlDataAccess.LoadData<AccountingOverviewModel, dynamic>(StoredProcedureNames.LoadAccountingDetailsByDate, new { FromDate, ToDate });

	public static async Task<AccountingOverviewModel> LoadAccountingOverviewByAccountingId(int AccountingId) =>
		(await SqlDataAccess.LoadData<AccountingOverviewModel, dynamic>(StoredProcedureNames.LoadAccountingOverviewByAccountingId, new { AccountingId })).FirstOrDefault();

	public static async Task<List<TrialBalanceModel>> LoadTrialBalanceByDate(DateTime FromDate, DateTime ToDate) =>
		await SqlDataAccess.LoadData<TrialBalanceModel, dynamic>(StoredProcedureNames.LoadTrialBalanceByDate, new { FromDate, ToDate });

	public static async Task<int> SaveAccountingTransaction(AccountingModel accounting, List<AccountingCartModel> cart)
	{
		bool update = accounting.Id > 0;

		accounting.Status = true;
		accounting.GeneratedModule = GeneratedModules.FinancialAccounting.ToString();
		accounting.FinancialYearId = (await FinancialYearData.LoadFinancialYearByDate(accounting.AccountingDate)).Id;
		accounting.ReferenceNo = await GenerateCodes.GenerateAccountingReferenceNo(accounting.VoucherId, accounting.AccountingDate);

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
				Debit = item.Debit,
				Credit = item.Credit,
				Remarks = item.Remarks ?? string.Empty,
				Status = true
			});
	}
}
