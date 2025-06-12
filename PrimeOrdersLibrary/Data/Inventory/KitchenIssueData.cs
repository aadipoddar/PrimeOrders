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
}
