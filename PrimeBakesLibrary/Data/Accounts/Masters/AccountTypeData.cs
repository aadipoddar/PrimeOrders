using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Data.Accounts.Masters;

public static class AccountTypeData
{
	public static async Task InsertAccountType(AccountTypeModel accountType) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertAccountType, accountType);
}
