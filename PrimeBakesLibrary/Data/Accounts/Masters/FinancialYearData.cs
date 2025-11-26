using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Data.Accounts.Masters;

public static class FinancialYearData
{
    public static async Task<int> InsertFinancialYear(FinancialYearModel financialYear) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertFinancialYear, financialYear)).FirstOrDefault();

    public static async Task<FinancialYearModel> LoadFinancialYearByDateTime(DateTime TransactionDateTime) =>
        (await SqlDataAccess.LoadData<FinancialYearModel, dynamic>(StoredProcedureNames.LoadFinancialYearByDateTime, new { TransactionDateTime })).FirstOrDefault();
}