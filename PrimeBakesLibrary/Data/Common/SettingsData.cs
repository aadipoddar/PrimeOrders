using PrimeBakesLibrary.Models.Common;

namespace PrimeBakesLibrary.Data.Common;

public static class SettingsData
{
	public static async Task<SettingsModel> LoadSettingsByKey(string Key) =>
		(await SqlDataAccess.LoadData<SettingsModel, dynamic>(StoredProcedureNames.LoadSettingsByKey, new { Key })).FirstOrDefault();

	public static async Task UpdateSettings(SettingsModel settingsModel) =>
			await SqlDataAccess.SaveData(StoredProcedureNames.UpdateSettings, settingsModel);

	public static async Task ResetSettings() =>
			await SqlDataAccess.ExecuteProcedure(StoredProcedureNames.ResetSettings);
}
