using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Data.Accounts.Masters;

public static class GroupData
{
	public static async Task InsertGroup(GroupModel group) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertGroup, group);
}
