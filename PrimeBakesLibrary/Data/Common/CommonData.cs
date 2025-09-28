namespace PrimeBakesLibrary.Data.Common;

public static class CommonData
{
	public static async Task<List<T>> LoadTableData<T>(string TableName) where T : new() =>
		await SqlDataAccess.LoadData<T, dynamic>(StoredProcedureNames.LoadTableData, new { TableName });

	public static async Task<T> LoadTableDataById<T>(string TableName, int Id) where T : new() =>
			(await SqlDataAccess.LoadData<T, dynamic>(StoredProcedureNames.LoadTableDataById, new { TableName, Id })).FirstOrDefault();

	public static async Task<List<T>> LoadTableDataByStatus<T>(string TableName, bool Status = true) where T : new() =>
			await SqlDataAccess.LoadData<T, dynamic>(StoredProcedureNames.LoadTableDataByStatus, new { TableName, Status });
}
