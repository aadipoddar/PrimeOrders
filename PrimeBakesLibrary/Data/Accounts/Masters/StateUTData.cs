using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Data.Accounts.Masters;

public static class StateUTData
{
    public static async Task<int> InsertStateUT(StateUTModel state) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertStateUT, state)).FirstOrDefault();
}