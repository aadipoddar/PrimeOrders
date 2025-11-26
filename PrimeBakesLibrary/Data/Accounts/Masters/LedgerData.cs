using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Data.Accounts.Masters;

public static class LedgerData
{
    public static async Task<int> InsertLedger(LedgerModel ledger) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertLedger, ledger)).FirstOrDefault();

    public static async Task<LedgerModel> LoadLedgerByLocation(int LocationId) =>
        (await SqlDataAccess.LoadData<LedgerModel, dynamic>(StoredProcedureNames.LoadLedgerByLocation, new { LocationId })).FirstOrDefault();
}
