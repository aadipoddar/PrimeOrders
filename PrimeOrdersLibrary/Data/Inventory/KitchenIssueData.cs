using PrimeOrdersLibrary.Models.Inventory;

namespace PrimeOrdersLibrary.Data.Inventory;

public static class KitchenIssueData
{
	public static async Task<int> InsertKitchenIssue(KitchenIssueModel kitchenIssue) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertKitchenIssue, kitchenIssue)).FirstOrDefault();

	public static async Task InsertKitchenIssueDetail(KitchenIssueDetailModel kitchenIssueDetail) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertKitchenIssueDetail, kitchenIssueDetail);

	public static async Task<KitchenIssueModel> LoadLastKitchenIssueByLocation(int LocationId) =>
		(await SqlDataAccess.LoadData<KitchenIssueModel, dynamic>(StoredProcedureNames.LoadLastKitchenIssueByLocation, new { LocationId })).FirstOrDefault();

	public static async Task<List<KitchenIssueOverviewModel>> LoadKitcheIssueDetailsByDate(DateTime FromDate, DateTime ToDate) =>
		await SqlDataAccess.LoadData<KitchenIssueOverviewModel, dynamic>(StoredProcedureNames.LoadKitcheIssueDetailsByDate, new { FromDate, ToDate });

	public static async Task<List<KitchenIssueDetailModel>> LoadKitchenIssueDetailByKitchenIssue(int KitchenIssueId) =>
		await SqlDataAccess.LoadData<KitchenIssueDetailModel, dynamic>(StoredProcedureNames.LoadKitchenIssueDetailByKitchenIssue, new { KitchenIssueId });
}
