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
}
