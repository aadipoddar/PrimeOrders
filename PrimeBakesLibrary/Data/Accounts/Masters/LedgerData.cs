using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Data.Accounts.Masters;

public static class LedgerData
{
	public static async Task InsertLedger(LedgerModel ledger) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertLedger, ledger);

	public static async Task<LedgerModel> LoadLedgerByLocation(int LocationId) =>
		(await SqlDataAccess.LoadData<LedgerModel, dynamic>(StoredProcedureNames.LoadLedgerByLocation, new { LocationId })).FirstOrDefault();

	public static async Task<List<LedgerOverviewModel>> LoadLedgerDetailsByDateLedger(DateTime FromDate, DateTime ToDate, int LedgerId) =>
		await SqlDataAccess.LoadData<LedgerOverviewModel, dynamic>(StoredProcedureNames.LoadLedgerDetailsByDateLedger, new { FromDate, ToDate, LedgerId });
}
