using PrimeOrdersLibrary.Models.Accounts;

namespace PrimeOrdersLibrary.Data.Accounts;

public static class LedgerData
{
	public static async Task InsertLedger(LedgerModel ledger) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertLedger, ledger);
}
