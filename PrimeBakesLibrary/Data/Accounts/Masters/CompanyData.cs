using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Data.Accounts.Masters;

public static class CompanyData
{
    public static async Task<int> InsertCompany(CompanyModel company) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertCompany, company)).FirstOrDefault();
}
