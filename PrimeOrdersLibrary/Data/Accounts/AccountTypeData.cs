using PrimeOrdersLibrary.Models.Accounts;

namespace PrimeOrdersLibrary.Data.Accounts;

public static class AccountTypeData
{
	public static async Task InsertAccountType(AccountTypeModel accountType) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertAccountType, accountType);
}
