using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Purchase;
using PrimeBakesLibrary.Models.Inventory.Stock;

namespace PrimeBakesLibrary.Data.Inventory.Purchase;

public static class PurchaseReturnData
{
	public static async Task<int> InsertPurchaseReturn(PurchaseReturnModel purchaseReturn) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertPurchaseReturn, purchaseReturn)).FirstOrDefault();

	public static async Task<int> InsertPurchaseReturnDetail(PurchaseReturnDetailModel purchaseReturnDetail) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertPurchaseReturnDetail, purchaseReturnDetail)).FirstOrDefault();

	public static async Task<List<PurchaseReturnDetailModel>> LoadPurchaseReturnDetailByPurchaseReturn(int PurchaseReturnId) =>
		await SqlDataAccess.LoadData<PurchaseReturnDetailModel, dynamic>(StoredProcedureNames.LoadPurchaseReturnDetailByPurchaseReturn, new { PurchaseReturnId });

	public static async Task<List<PurchaseReturnOverviewModel>> LoadPurchaseReturnOverviewByDate(DateTime StartDate, DateTime EndDate, bool OnlyActive = true) =>
		await SqlDataAccess.LoadData<PurchaseReturnOverviewModel, dynamic>(StoredProcedureNames.LoadPurchaseReturnOverviewByDate, new { StartDate, EndDate, OnlyActive });

	public static async Task<List<PurchaseReturnItemOverviewModel>> LoadPurchaseReturnItemOverviewByDate(DateTime StartDate, DateTime EndDate) =>
		await SqlDataAccess.LoadData<PurchaseReturnItemOverviewModel, dynamic>(StoredProcedureNames.LoadPurchaseReturnItemOverviewByDate, new { StartDate, EndDate });

	public static async Task DeletePurchaseReturn(int purchaseReturnId)
	{
		var purchaseReturn = await CommonData.LoadTableDataById<PurchaseReturnModel>(TableNames.PurchaseReturn, purchaseReturnId);
		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, purchaseReturn.FinancialYearId);
		if (financialYear is null || financialYear.Locked || financialYear.Status == false)
			throw new InvalidOperationException("Cannot delete purchase return transaction as the financial year is locked.");

		if (purchaseReturn is not null)
		{
			purchaseReturn.Status = false;
			await InsertPurchaseReturn(purchaseReturn);
			await RawMaterialStockData.DeleteRawMaterialStockByTypeIdNo(StockType.PurchaseReturn.ToString(), purchaseReturn.Id, purchaseReturn.TransactionNo);
			var existingAccounting = await AccountingData.LoadAccountingByTransactionNo(purchaseReturn.TransactionNo);
			if (existingAccounting is not null && existingAccounting.Id > 0)
			{
				existingAccounting.Status = false;
				await AccountingData.InsertAccounting(existingAccounting);
			}
		}
	}

	public static async Task RecoverPurchaseReturnTransaction(PurchaseReturnModel purchaseReturn)
	{
		var purchaseDetails = await LoadPurchaseReturnDetailByPurchaseReturn(purchaseReturn.Id);
		List<PurchaseReturnItemCartModel> purchaseItemCarts = [];

		foreach (var item in purchaseDetails)
			purchaseItemCarts.Add(new()
			{
				ItemId = item.RawMaterialId,
				ItemName = "",
				UnitOfMeasurement = item.UnitOfMeasurement,
				Quantity = item.Quantity,
				Rate = item.Rate,
				BaseTotal = item.BaseTotal,
				DiscountPercent = item.DiscountPercent,
				DiscountAmount = item.DiscountAmount,
				AfterDiscount = item.AfterDiscount,
				CGSTPercent = item.CGSTPercent,
				CGSTAmount = item.CGSTAmount,
				SGSTPercent = item.SGSTPercent,
				SGSTAmount = item.SGSTAmount,
				IGSTPercent = item.IGSTPercent,
				IGSTAmount = item.IGSTAmount,
				InclusiveTax = item.InclusiveTax,
				TotalTaxAmount = item.TotalTaxAmount,
				Total = item.Total,
				NetRate = item.NetRate,
				Remarks = item.Remarks
			});

		await SavePurchaseReturnTransaction(purchaseReturn, purchaseItemCarts);
	}

	public static async Task<int> SavePurchaseReturnTransaction(PurchaseReturnModel purchaseReturn, List<PurchaseReturnItemCartModel> purchaseReturnDetails)
	{
		bool update = purchaseReturn.Id > 0;

		if (update)
		{
			var existingPurchaseReturn = await CommonData.LoadTableDataById<PurchaseReturnModel>(TableNames.PurchaseReturn, purchaseReturn.Id);
			var updateFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, existingPurchaseReturn.FinancialYearId);
			if (updateFinancialYear is null || updateFinancialYear.Locked || updateFinancialYear.Status == false)
				throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");
		}

		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, purchaseReturn.FinancialYearId);
		if (financialYear is null || financialYear.Locked || financialYear.Status == false)
			throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");

		purchaseReturn.Id = await InsertPurchaseReturn(purchaseReturn);
		await SavePurchaseReturnDetail(purchaseReturn, purchaseReturnDetails, update);
		await SaveRawMaterialStock(purchaseReturn, purchaseReturnDetails, update);
		await SaveAccounting(purchaseReturn, update);

		return purchaseReturn.Id;
	}

	private static async Task SavePurchaseReturnDetail(PurchaseReturnModel purchaseReturn, List<PurchaseReturnItemCartModel> purchaseReturnDetails, bool update)
	{
		if (update)
		{
			var existingPurchaseDetails = await LoadPurchaseReturnDetailByPurchaseReturn(purchaseReturn.Id);
			foreach (var item in existingPurchaseDetails)
			{
				item.Status = false;
				await InsertPurchaseReturnDetail(item);
			}
		}

		foreach (var item in purchaseReturnDetails)
			await InsertPurchaseReturnDetail(new()
			{
				Id = 0,
				PurchaseReturnId = purchaseReturn.Id,
				RawMaterialId = item.ItemId,
				Quantity = item.Quantity,
				UnitOfMeasurement = item.UnitOfMeasurement,
				Rate = item.Rate,
				BaseTotal = item.BaseTotal,
				DiscountPercent = item.DiscountPercent,
				DiscountAmount = item.DiscountAmount,
				AfterDiscount = item.AfterDiscount,
				CGSTPercent = item.CGSTPercent,
				CGSTAmount = item.CGSTAmount,
				SGSTPercent = item.SGSTPercent,
				SGSTAmount = item.SGSTAmount,
				IGSTPercent = item.IGSTPercent,
				IGSTAmount = item.IGSTAmount,
				TotalTaxAmount = item.TotalTaxAmount,
				InclusiveTax = item.InclusiveTax,
				NetRate = item.NetRate,
				Total = item.Total,
				Remarks = item.Remarks,
				Status = true
			});
	}

	private static async Task SaveRawMaterialStock(PurchaseReturnModel purchaseReturn, List<PurchaseReturnItemCartModel> cart, bool update)
	{
		if (update)
			await RawMaterialStockData.DeleteRawMaterialStockByTypeIdNo(StockType.PurchaseReturn.ToString(), purchaseReturn.Id, purchaseReturn.TransactionNo);

		foreach (var item in cart)
			await RawMaterialStockData.InsertRawMaterialStock(new()
			{
				Id = 0,
				RawMaterialId = item.ItemId,
				Quantity = -item.Quantity,
				NetRate = item.NetRate,
				TransactionId = purchaseReturn.Id,
				Type = StockType.PurchaseReturn.ToString(),
				TransactionNo = purchaseReturn.TransactionNo,
				TransactionDate = DateOnly.FromDateTime(purchaseReturn.TransactionDateTime)
			});
	}

	private static async Task SaveAccounting(PurchaseReturnModel purchaseReturn, bool update)
	{
		if (update)
		{
			var existingAccounting = await AccountingData.LoadAccountingByTransactionNo(purchaseReturn.TransactionNo);
			if (existingAccounting is not null && existingAccounting.Id > 0)
			{
				existingAccounting.Status = false;
				await AccountingData.InsertAccounting(existingAccounting);
			}
		}

		var purchaseReturnOverview = await CommonData.LoadTableDataById<PurchaseReturnOverviewModel>(ViewNames.PurchaseReturnOverview, purchaseReturn.Id);
		if (purchaseReturnOverview.TotalAmount <= 0 && purchaseReturnOverview.TotalTaxAmount <= 0)
			return;

		int accountingId = await AccountingData.InsertAccounting(new()
		{
			Id = 0,
			TransactionNo = purchaseReturn.TransactionNo,
			AccountingDate = DateOnly.FromDateTime(purchaseReturn.TransactionDateTime),
			FinancialYearId = (await FinancialYearData.LoadFinancialYearByDateTime(purchaseReturn.TransactionDateTime)).Id,
			VoucherId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseReturnVoucherId)).Value),
			Remarks = purchaseReturn.Remarks,
			UserId = purchaseReturn.CreatedBy,
			GeneratedModule = GeneratedModules.PurchaseReturn.ToString(),
			CreatedAt = await CommonData.LoadCurrentDateTime(),
			Status = true
		});

		await SaveAccountingDetails(purchaseReturnOverview, accountingId);
	}

	private static async Task SaveAccountingDetails(PurchaseReturnOverviewModel purchaseReturnOverview, int accountingId)
	{
		// Supplier Account Posting (Debit)
		if (purchaseReturnOverview.TotalAmount > 0)
			await AccountingData.InsertAccountingDetails(new()
			{
				Id = 0,
				AccountingId = accountingId,
				LedgerId = purchaseReturnOverview.PartyId,
				ReferenceId = purchaseReturnOverview.Id,
				ReferenceType = ReferenceTypes.PurchaseReturn.ToString(),
				Debit = purchaseReturnOverview.TotalAmount,
				Credit = null,
				Remarks = $"Party Account Posting For Purchase Bill {purchaseReturnOverview.TransactionNo}",
				Status = true
			});

		// Purchase Account Posting (Credit)
		if (purchaseReturnOverview.TotalAmount - purchaseReturnOverview.TotalTaxAmount > 0)
			await AccountingData.InsertAccountingDetails(new()
			{
				Id = 0,
				AccountingId = accountingId,
				LedgerId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseLedgerId)).Value),
				ReferenceId = purchaseReturnOverview.Id,
				ReferenceType = ReferenceTypes.PurchaseReturn.ToString(),
				Debit = null,
				Credit = purchaseReturnOverview.TotalAmount - purchaseReturnOverview.TotalTaxAmount,
				Remarks = $"Purchase Account Posting For Purchase Bill {purchaseReturnOverview.TransactionNo}",
				Status = true
			});

		// GST Account Posting (Credit)
		if (purchaseReturnOverview.TotalTaxAmount > 0)
			await AccountingData.InsertAccountingDetails(new()
			{
				Id = 0,
				AccountingId = accountingId,
				LedgerId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.GSTLedgerId)).Value),
				ReferenceId = purchaseReturnOverview.Id,
				ReferenceType = ReferenceTypes.PurchaseReturn.ToString(),
				Debit = null,
				Credit = purchaseReturnOverview.TotalTaxAmount,
				Remarks = $"GST Account Posting For Purchase Bill {purchaseReturnOverview.TransactionNo}",
				Status = true
			});
	}
}