using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Data.Accounts.Masters;

public static class FinancialYearData
{
	public static async Task InsertFinancialYear(FinancialYearModel financialYear) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertFinancialYear, financialYear);

	public static async Task<FinancialYearModel> LoadFinancialYearByDate(DateOnly Date) =>
		(await SqlDataAccess.LoadData<FinancialYearModel, dynamic>(StoredProcedureNames.LoadFinancialYearByDate, new { Date })).FirstOrDefault();
}