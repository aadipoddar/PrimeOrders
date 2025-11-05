using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Data.Accounts.Masters;

public static class AccountTypeData
{
	public static async Task<int> InsertAccountType(AccountTypeModel accountType) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertAccountType, accountType)).FirstOrDefault();
}
