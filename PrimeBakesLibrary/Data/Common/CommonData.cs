namespace PrimeBakesLibrary.Data.Common;

public static class CommonData
{
	public static async Task<List<T>> LoadTableData<T>(string TableName) where T : new() =>
			await SqlDataAccess.LoadData<T, dynamic>(StoredProcedureNames.LoadTableData, new { TableName });

	public static async Task<T> LoadTableDataById<T>(string TableName, int Id) where T : new() =>
		(await SqlDataAccess.LoadData<T, dynamic>(StoredProcedureNames.LoadTableDataById, new { TableName, Id })).FirstOrDefault();

	public static async Task<List<T>> LoadTableDataByStatus<T>(string TableName, bool Status = true) where T : new() =>
		await SqlDataAccess.LoadData<T, dynamic>(StoredProcedureNames.LoadTableDataByStatus, new { TableName, Status });

	public static async Task<List<T>> LoadTableDataByMasterId<T>(string TableName, int MasterId) where T : new() =>
		await SqlDataAccess.LoadData<T, dynamic>(StoredProcedureNames.LoadTableDataByMasterId, new { TableName, MasterId });

	public static async Task<T> LoadTableDataByCode<T>(string TableName, string Code) where T : new() =>
		(await SqlDataAccess.LoadData<T, dynamic>(StoredProcedureNames.LoadTableDataByCode, new { TableName, Code })).FirstOrDefault();

	public static async Task<T> LoadTableDataByTransactionNo<T>(string TableName, string TransactionNo) where T : new() =>
		(await SqlDataAccess.LoadData<T, dynamic>(StoredProcedureNames.LoadTableDataByTransactionNo, new { TableName, TransactionNo })).FirstOrDefault();

	public static async Task<List<T>> LoadTableDataByDate<T>(string TableName, DateTime StartDate, DateTime EndDate) where T : new() =>
	await SqlDataAccess.LoadData<T, dynamic>(StoredProcedureNames.LoadTableDataByDate, new { TableName, StartDate, EndDate });

	public static async Task<T> LoadLastTableDataByFinancialYear<T>(string TableName, int FinancialYearId) where T : new() =>
		(await SqlDataAccess.LoadData<T, dynamic>(StoredProcedureNames.LoadLastTableDataByFinancialYear, new { TableName, FinancialYearId })).FirstOrDefault();

	public static async Task<T> LoadLastTableDataByLocationFinancialYear<T>(string TableName, int LocationId, int FinancialYearId) where T : new() =>
		(await SqlDataAccess.LoadData<T, dynamic>(StoredProcedureNames.LoadLastTableDataByLocationFinancialYear, new { TableName, LocationId, FinancialYearId })).FirstOrDefault();

	public static async Task<DateTime> LoadCurrentDateTime() =>
		(await SqlDataAccess.LoadData<DateTime, dynamic>(StoredProcedureNames.LoadCurrentDateTime, new { })).FirstOrDefault();
}
