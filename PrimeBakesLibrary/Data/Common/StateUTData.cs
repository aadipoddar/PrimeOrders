using PrimeBakesLibrary.Models.Common;

namespace PrimeBakesLibrary.Data.Common;

public static class StateUTData
{
	public static async Task<int> InsertStateUT(StateUTModel state) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertStateUT, state)).FirstOrDefault();
}