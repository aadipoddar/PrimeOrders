using PrimeOrdersLibrary.Models.Accounts.Masters;

namespace PrimeOrdersLibrary.Data.Accounts.Masters;

public static class AccountTypeData
{
	public static async Task InsertAccountType(AccountTypeModel accountType) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertAccountType, accountType);
}
