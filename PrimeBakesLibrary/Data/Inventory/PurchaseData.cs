using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Inventory.Stock;

namespace PrimeBakesLibrary.Data.Inventory;

public class PurchaseData
{
	public static async Task<int> InsertPurchase(PurchaseModel purchase) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertPurchase, purchase)).FirstOrDefault();

	public static async Task InsertPurchaseDetail(PurchaseDetailModel purchaseDetail) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertPurchaseDetail, purchaseDetail);

	public static async Task<List<PurchaseDetailModel>> LoadPurchaseDetailByPurchase(int PurchaseId) =>
		await SqlDataAccess.LoadData<PurchaseDetailModel, dynamic>(StoredProcedureNames.LoadPurchaseDetailByPurchase, new { PurchaseId });

	public static async Task<List<PurchaseOverviewModel>> LoadPurchaseDetailsByDate(DateTime FromDate, DateTime ToDate) =>
		await SqlDataAccess.LoadData<PurchaseOverviewModel, dynamic>(StoredProcedureNames.LoadPurchaseDetailsByDate, new { FromDate, ToDate });

	public static async Task<PurchaseOverviewModel> LoadPurchaseOverviewByPurchaseId(int PurchaseId) =>
		(await SqlDataAccess.LoadData<PurchaseOverviewModel, dynamic>(StoredProcedureNames.LoadPurchaseOverviewByPurchaseId, new { PurchaseId })).FirstOrDefault();

	public static async Task<int> SavePurchase(PurchaseModel purchase, List<PurchaseRawMaterialCartModel> cart)
	{
		bool update = purchase.Id > 0;

		purchase.Status = true;
		purchase.CreatedAt = DateTime.Now;
		purchase.BillDateTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateOnly.FromDateTime(purchase.BillDateTime)
			.ToDateTime(new TimeOnly(purchase.BillDateTime.Hour, purchase.BillDateTime.Minute, purchase.BillDateTime.Second)),
			"India Standard Time");

		purchase.Id = await InsertPurchase(purchase);
		await SavePurchaseDetail(purchase, cart, update);
		await SaveRawMaterialStock(purchase, cart, update);
		await InsertAccounting(purchase, update);

		return purchase.Id;
	}

	private static async Task SavePurchaseDetail(PurchaseModel purchase, List<PurchaseRawMaterialCartModel> cart, bool update)
	{
		if (update)
		{
			var existingPurchaseDetails = await LoadPurchaseDetailByPurchase(purchase.Id);
			foreach (var item in existingPurchaseDetails)
			{
				item.Status = false;
				await InsertPurchaseDetail(item);
			}
		}

		foreach (var item in cart)
			await InsertPurchaseDetail(new()
			{
				Id = 0,
				PurchaseId = purchase.Id,
				RawMaterialId = item.RawMaterialId,
				Quantity = item.Quantity,
				MeasurementUnit = item.MeasurementUnit,
				Rate = item.Rate,
				BaseTotal = item.BaseTotal,
				DiscPercent = item.DiscPercent,
				DiscAmount = item.DiscAmount,
				AfterDiscount = item.AfterDiscount,
				CGSTPercent = item.CGSTPercent,
				CGSTAmount = item.CGSTAmount,
				SGSTPercent = item.SGSTPercent,
				SGSTAmount = item.SGSTAmount,
				IGSTPercent = item.IGSTPercent,
				IGSTAmount = item.IGSTAmount,
				Total = item.Total,
				NetRate = item.NetRate,
				Status = true
			});
	}

	private static async Task SaveRawMaterialStock(PurchaseModel purchase, List<PurchaseRawMaterialCartModel> cart, bool update)
	{
		if (update)
			await RawMaterialStockData.DeleteRawMaterialStockByTransactionNo(purchase.BillNo);

		foreach (var item in cart)
		{
			await RawMaterialStockData.InsertRawMaterialStock(new()
			{
				Id = 0,
				RawMaterialId = item.RawMaterialId,
				Quantity = item.Quantity,
				NetRate = item.NetRate,
				Type = StockType.Purchase.ToString(),
				TransactionNo = purchase.BillNo,
				TransactionDate = DateOnly.FromDateTime(purchase.BillDateTime),
				LocationId = 1 // Purchases are always to primary location
			});
		}
	}

	private static async Task InsertAccounting(PurchaseModel purchase, bool update)
	{
		if (update)
		{
			var existingAccounting = await AccountingData.LoadAccountingByReferenceNo(purchase.BillNo);
			if (existingAccounting is not null && existingAccounting.Id > 0)
			{
				existingAccounting.Status = false;
				await AccountingData.InsertAccounting(existingAccounting);
			}
		}

		int accountingId = await AccountingData.InsertAccounting(new()
		{
			Id = 0,
			ReferenceNo = purchase.BillNo,
			AccountingDate = DateOnly.FromDateTime(purchase.BillDateTime),
			FinancialYearId = (await FinancialYearData.LoadFinancialYearByDate(DateOnly.FromDateTime(purchase.BillDateTime))).Id,
			VoucherId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseVoucherId)).Value),
			Remarks = purchase.Remarks,
			UserId = purchase.UserId,
			GeneratedModule = GeneratedModules.Purchase.ToString(),
			CreatedAt = DateTime.Now,
			Status = true
		});

		await InsertAccountingDetails(purchase, accountingId);
	}

	private static async Task InsertAccountingDetails(PurchaseModel purchase, int accountingId)
	{
		var purchaseOverview = await LoadPurchaseOverviewByPurchaseId(purchase.Id);

		// Supplier Account Posting (Credit)
		await AccountingData.InsertAccountingDetails(new()
		{
			Id = 0,
			AccountingId = accountingId,
			LedgerId = purchaseOverview.SupplierId,
			Debit = null,
			Credit = purchaseOverview.Total,
			Remarks = $"Supplier Account Posting For Purchase Bill {purchaseOverview.BillNo}",
			Status = true
		});

		// Purchase Account Posting (Debit)
		await AccountingData.InsertAccountingDetails(new()
		{
			Id = 0,
			AccountingId = accountingId,
			LedgerId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseLedgerId)).Value),
			Debit = purchaseOverview.Total - purchaseOverview.TotalTaxAmount,
			Credit = null,
			Remarks = $"Purchase Account Posting For Purchase Bill {purchaseOverview.BillNo}",
			Status = true
		});

		// GST Account Posting (Debit)
		if (purchaseOverview.TotalTaxAmount > 0)
		{
			await AccountingData.InsertAccountingDetails(new()
			{
				Id = 0,
				AccountingId = accountingId,
				LedgerId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.GSTLedgerId)).Value),
				Debit = purchaseOverview.TotalTaxAmount,
				Credit = null,
				Remarks = $"GST Account Posting For Purchase Bill {purchaseOverview.BillNo}",
				Status = true
			});
		}
	}
}