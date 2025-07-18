using PrimeOrdersLibrary.Models.Accounts;

namespace PrimeOrdersLibrary.Data.Accounts;

public static class FinancialYearData
{
	public static async Task InsertFinancialYear(FinancialYearModel financialYear) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertFinancialYear, financialYear);
}