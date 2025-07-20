using PrimeOrdersLibrary.Models.Accounts.Masters;

namespace PrimeOrdersLibrary.Data.Accounts.Masters;

public static class GroupData
{
	public static async Task InsertGroup(GroupModel group) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertGroup, group);
}
