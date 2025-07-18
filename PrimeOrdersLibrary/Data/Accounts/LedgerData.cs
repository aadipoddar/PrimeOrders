using PrimeOrdersLibrary.Models.Accounts;

namespace PrimeOrdersLibrary.Data.Accounts;

public static class LedgerData
{
	public static async Task InsertLedger(LedgerModel ledger) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertLedger, ledger);

	public static async Task<LedgerModel> LoadLedgerByLocation(int LocationId) =>
		(await SqlDataAccess.LoadData<LedgerModel, dynamic>(StoredProcedureNames.LoadLedgerByLocation, new { LocationId })).FirstOrDefault();
}
