using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Common;

namespace PrimeBakesLibrary.Data.Common;

public static class StateData
{
	public static async Task InsertState(StateModel state) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertState, state);
}