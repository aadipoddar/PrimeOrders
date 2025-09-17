using PrimeOrdersLibrary.Models.Inventory;

namespace PrimeOrdersLibrary.Data.Inventory.Kitchen;

public static class KitchenIssueData
{
	public static async Task<int> InsertKitchenIssue(KitchenIssueModel kitchenIssue) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertKitchenIssue, kitchenIssue)).FirstOrDefault();

	public static async Task InsertKitchenIssueDetail(KitchenIssueDetailModel kitchenIssueDetail) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertKitchenIssueDetail, kitchenIssueDetail);

	public static async Task<KitchenIssueModel> LoadLastKitchenIssueByLocation(int LocationId) =>
		(await SqlDataAccess.LoadData<KitchenIssueModel, dynamic>(StoredProcedureNames.LoadLastKitchenIssueByLocation, new { LocationId })).FirstOrDefault();

	public static async Task<List<KitchenIssueOverviewModel>> LoadKitchenIssueDetailsByDate(DateTime FromDate, DateTime ToDate) =>
		await SqlDataAccess.LoadData<KitchenIssueOverviewModel, dynamic>(StoredProcedureNames.LoadKitchenIssueDetailsByDate, new { FromDate, ToDate });

	public static async Task<List<KitchenIssueDetailModel>> LoadKitchenIssueDetailByKitchenIssue(int KitchenIssueId) =>
		await SqlDataAccess.LoadData<KitchenIssueDetailModel, dynamic>(StoredProcedureNames.LoadKitchenIssueDetailByKitchenIssue, new { KitchenIssueId });

	public static async Task<KitchenIssueOverviewModel> LoadKitchenIssueOverviewByKitchenIssueId(int KitchenIssueId) =>
		(await SqlDataAccess.LoadData<KitchenIssueOverviewModel, dynamic>(StoredProcedureNames.LoadKitchenIssueOverviewByKitchenIssueId, new { KitchenIssueId })).FirstOrDefault();
}
