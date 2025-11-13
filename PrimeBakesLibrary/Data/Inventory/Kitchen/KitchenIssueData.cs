using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.Data.Notification;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Inventory.Stock;

namespace PrimeBakesLibrary.Data.Inventory.Kitchen;

public static class KitchenIssueData
{
	public static async Task<int> InsertKitchenIssue(KitchenIssueModel kitchenIssue) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertKitchenIssue, kitchenIssue)).FirstOrDefault();

	public static async Task<int> InsertKitchenIssueDetail(KitchenIssueDetailModel kitchenIssueDetail) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertKitchenIssueDetail, kitchenIssueDetail)).FirstOrDefault();

	public static async Task<List<KitchenIssueDetailModel>> LoadKitchenIssueDetailByKitchenIssue(int KitchenIssueId) =>
		await SqlDataAccess.LoadData<KitchenIssueDetailModel, dynamic>(StoredProcedureNames.LoadKitchenIssueDetailByKitchenIssue, new { KitchenIssueId });

	public static async Task<List<KitchenIssueOverviewModel>> LoadKitchenIssueOverviewByDate(DateTime StartDate, DateTime EndDate, bool OnlyActive = true) =>
		await SqlDataAccess.LoadData<KitchenIssueOverviewModel, dynamic>(StoredProcedureNames.LoadKitchenIssueOverviewByDate, new { StartDate, EndDate, OnlyActive });

	public static async Task<List<KitchenIssueItemOverviewModel>> LoadKitchenIssueItemOverviewByDate(DateTime StartDate, DateTime EndDate) =>
		await SqlDataAccess.LoadData<KitchenIssueItemOverviewModel, dynamic>(StoredProcedureNames.LoadKitchenIssueItemOverviewByDate, new { StartDate, EndDate });

	public static async Task DeleteKitchenIssue(int kitchenIssueId)
	{
		var kitchenIssue = await CommonData.LoadTableDataById<KitchenIssueModel>(TableNames.KitchenIssue, kitchenIssueId);
		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, kitchenIssue.FinancialYearId);
		if (financialYear is null || financialYear.Locked || financialYear.Status == false)
			throw new InvalidOperationException("Cannot delete kitchen issue transaction as the financial year is locked.");

		if (kitchenIssue is not null)
		{
			kitchenIssue.Status = false;
			await InsertKitchenIssue(kitchenIssue);
			await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(StockType.KitchenIssue.ToString(), kitchenIssue.Id);
		}
	}

	public static async Task RecoverKitchenIssueTransaction(KitchenIssueModel kitchenIssue)
	{
		var kitchenIssueDetails = await LoadKitchenIssueDetailByKitchenIssue(kitchenIssue.Id);
		List<KitchenIssueItemCartModel> kitchenIssueItemCarts = [];

		foreach (var item in kitchenIssueDetails)
			kitchenIssueItemCarts.Add(new()
			{
				ItemId = item.RawMaterialId,
				ItemName = "",
				UnitOfMeasurement = item.UnitOfMeasurement,
				Quantity = item.Quantity,
				Rate = item.Rate,
				Total = item.Total,
				Remarks = item.Remarks
			});

		await SaveKitchenIssueTransaction(kitchenIssue, kitchenIssueItemCarts);
	}

	public static async Task<int> SaveKitchenIssueTransaction(KitchenIssueModel kitchenIssue, List<KitchenIssueItemCartModel> kitchenIssueDetails)
	{
		bool update = kitchenIssue.Id > 0;

		if (update)
		{
			var existingkitchenIssue = await CommonData.LoadTableDataById<KitchenIssueModel>(TableNames.KitchenIssue, kitchenIssue.Id);
			var updateFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, existingkitchenIssue.FinancialYearId);
			if (updateFinancialYear is null || updateFinancialYear.Locked || updateFinancialYear.Status == false)
				throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");
		}

		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, kitchenIssue.FinancialYearId);
		if (financialYear is null || financialYear.Locked || financialYear.Status == false)
			throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");

		kitchenIssue.Id = await InsertKitchenIssue(kitchenIssue);
		await SaveKitchenIssueDetail(kitchenIssue, kitchenIssueDetails, update);
		await SaveRawMaterialStock(kitchenIssue, kitchenIssueDetails, update);
		// await SendNotification.SendKitchenIssueNotificationMainLocationAdminInventory(kitchenIssue.Id);

		return kitchenIssue.Id;
	}

	private static async Task SaveKitchenIssueDetail(KitchenIssueModel kitchenIssue, List<KitchenIssueItemCartModel> kitchenIssueDetails, bool update)
	{
		if (update)
		{
			var existingKitchenIssueDetails = await LoadKitchenIssueDetailByKitchenIssue(kitchenIssue.Id);
			foreach (var item in existingKitchenIssueDetails)
			{
				item.Status = false;
				await InsertKitchenIssueDetail(item);
			}
		}

		foreach (var item in kitchenIssueDetails)
			await InsertKitchenIssueDetail(new()
			{
				Id = 0,
				KitchenIssueId = kitchenIssue.Id,
				RawMaterialId = item.ItemId,
				Quantity = item.Quantity,
				UnitOfMeasurement = item.UnitOfMeasurement,
				Rate = item.Rate,
				Total = item.Total,
				Remarks = item.Remarks,
				Status = true
			});
	}

	private static async Task SaveRawMaterialStock(KitchenIssueModel kitchenIssue, List<KitchenIssueItemCartModel> cart, bool update)
	{
		if (update)
			await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(StockType.KitchenIssue.ToString(), kitchenIssue.Id);

		foreach (var item in cart)
			await RawMaterialStockData.InsertRawMaterialStock(new()
			{
				Id = 0,
				RawMaterialId = item.ItemId,
				Quantity = -item.Quantity,
				NetRate = null,
				Type = StockType.KitchenIssue.ToString(),
				TransactionId = kitchenIssue.Id,
				TransactionNo = kitchenIssue.TransactionNo,
				TransactionDate = DateOnly.FromDateTime(kitchenIssue.TransactionDateTime)
			});
	}
}