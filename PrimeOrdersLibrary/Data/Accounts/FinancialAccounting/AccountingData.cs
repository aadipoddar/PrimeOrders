using PrimeOrdersLibrary.Models.Accounts.FinancialAccounting;

namespace PrimeOrdersLibrary.Data.Accounts.FinancialAccounting;

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
}
