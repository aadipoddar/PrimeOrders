using PrimeBakesLibrary.Models.Common;

namespace PrimeBakesLibrary.Data.Common;

public static class UserData
{
	public static async Task InsertUser(UserModel userModel) =>
			await SqlDataAccess.SaveData(StoredProcedureNames.InsertUser, userModel);

	public static async Task<UserModel> LoadUserByPasscode(int Passcode) =>
			(await SqlDataAccess.LoadData<UserModel, dynamic>(StoredProcedureNames.LoadUserByPasscode, new { Passcode })).FirstOrDefault();
}
