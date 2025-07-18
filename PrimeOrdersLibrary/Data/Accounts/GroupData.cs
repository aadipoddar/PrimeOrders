using PrimeOrdersLibrary.Models.Accounts;

namespace PrimeOrdersLibrary.Data.Accounts;

public static class GroupData
{
	public static async Task InsertGroup(GroupModel group) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertGroup, group);
}
