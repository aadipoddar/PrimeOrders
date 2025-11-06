using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Inventory.Purchase;
using PrimeBakesLibrary.Models.Inventory.Stock;

namespace PrimeBakesLibrary.Data.Inventory.Purchase;

public static class PurchaseData
{
	public static async Task<int> InsertPurchase(PurchaseModel purchase) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertPurchase, purchase)).FirstOrDefault();

	public static async Task<int> InsertPurchaseDetail(PurchaseDetailModel purchaseDetail) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertPurchaseDetail, purchaseDetail)).FirstOrDefault();

	public static async Task<List<PurchaseDetailModel>> LoadPurchaseDetailByPurchase(int PurchaseId) =>
		await SqlDataAccess.LoadData<PurchaseDetailModel, dynamic>(StoredProcedureNames.LoadPurchaseDetailByPurchase, new { PurchaseId });

	public static async Task<List<PurchaseOverviewModel>> LoadPurchaseOverviewByDate(DateTime StartDate, DateTime EndDate, bool OnlyActive = true) =>
		await SqlDataAccess.LoadData<PurchaseOverviewModel, dynamic>(StoredProcedureNames.LoadPurchaseOverviewByDate, new { StartDate, EndDate, OnlyActive });

	public static async Task<List<PurchaseItemOverviewModel>> LoadPurchaseItemOverviewByDate(DateTime StartDate, DateTime EndDate) =>
		await SqlDataAccess.LoadData<PurchaseItemOverviewModel, dynamic>(StoredProcedureNames.LoadPurchaseItemOverviewByDate, new { StartDate, EndDate });

	public static async Task<List<RawMaterialModel>> LoadRawMaterialByPartyPurchaseDateTime(int PartyId, DateTime PurchaseDateTime, bool OnlyActive = true) =>
		await SqlDataAccess.LoadData<RawMaterialModel, dynamic>(StoredProcedureNames.LoadRawMaterialByPartyPurchaseDateTime, new { PartyId, PurchaseDateTime, OnlyActive });

	public static async Task DeletePurchase(int purchaseId)
	{
		var purchase = await CommonData.LoadTableDataById<PurchaseModel>(TableNames.Purchase, purchaseId);
		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, purchase.FinancialYearId);
		if (financialYear is null || financialYear.Locked || financialYear.Status == false)
			throw new InvalidOperationException("Cannot delete purchase transaction as the financial year is locked.");

		if (purchase is not null)
		{
			purchase.Status = false;
			await InsertPurchase(purchase);
			await RawMaterialStockData.DeleteRawMaterialStockByTransactionNo(purchase.TransactionNo);

			var existingAccounting = await AccountingData.LoadAccountingByTransactionNo(purchase.TransactionNo);
			if (existingAccounting is not null && existingAccounting.Id > 0)
			{
				existingAccounting.Status = false;
				await AccountingData.InsertAccounting(existingAccounting);
			}
		}
	}

	public static async Task RecoverPurchaseTransaction(PurchaseModel purchase)
	{
		var purchaseDetails = await LoadPurchaseDetailByPurchase(purchase.Id);
		List<PurchaseItemCartModel> purchaseItemCarts = [];

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

		await SavePurchaseTransaction(purchase, purchaseItemCarts);
	}

	public static async Task<int> SavePurchaseTransaction(PurchaseModel purchase, List<PurchaseItemCartModel> purchaseDetails)
	{
		bool update = purchase.Id > 0;

		if (update)
		{
			var existingPurchase = await CommonData.LoadTableDataById<PurchaseModel>(TableNames.Purchase, purchase.Id);
			var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, existingPurchase.FinancialYearId);
			if (financialYear is null || financialYear.Locked || financialYear.Status == false)
				throw new InvalidOperationException("Cannot update purchase transaction as the financial year is locked.");
		}

		purchase.Id = await InsertPurchase(purchase);
		await SavePurchaseDetail(purchase, purchaseDetails, update);
		await SaveRawMaterialStock(purchase, purchaseDetails, update);
		await SaveAccounting(purchase, update);
		await UpdateRawMaterialRateAndUOMOnPurchase(purchaseDetails);

		return purchase.Id;
	}

	private static async Task SavePurchaseDetail(PurchaseModel purchase, List<PurchaseItemCartModel> purchaseDetails, bool update)
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

		foreach (var item in purchaseDetails)
			await InsertPurchaseDetail(new()
			{
				Id = 0,
				PurchaseId = purchase.Id,
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

	private static async Task SaveRawMaterialStock(PurchaseModel purchase, List<PurchaseItemCartModel> cart, bool update)
	{
		if (update)
			await RawMaterialStockData.DeleteRawMaterialStockByTransactionNo(purchase.TransactionNo);

		foreach (var item in cart)
			await RawMaterialStockData.InsertRawMaterialStock(new()
			{
				Id = 0,
				RawMaterialId = item.ItemId,
				Quantity = item.Quantity,
				NetRate = item.NetRate,
				Type = StockType.Purchase.ToString(),
				TransactionNo = purchase.TransactionNo,
				TransactionDate = DateOnly.FromDateTime(purchase.TransactionDateTime),
				LocationId = 1 // Purchases are always to primary location
			});
	}

	private static async Task SaveAccounting(PurchaseModel purchase, bool update)
	{
		if (update)
		{
			var existingAccounting = await AccountingData.LoadAccountingByTransactionNo(purchase.TransactionNo);
			if (existingAccounting is not null && existingAccounting.Id > 0)
			{
				existingAccounting.Status = false;
				await AccountingData.InsertAccounting(existingAccounting);
			}
		}

		var purchaseOverview = await CommonData.LoadTableDataById<PurchaseOverviewModel>(ViewNames.PurchaseOverview, purchase.Id);
		if (purchaseOverview.TotalAmount <= 0 && purchaseOverview.TotalTaxAmount <= 0)
			return;

		int accountingId = await AccountingData.InsertAccounting(new()
		{
			Id = 0,
			TransactionNo = purchase.TransactionNo,
			AccountingDate = DateOnly.FromDateTime(purchase.TransactionDateTime),
			FinancialYearId = (await FinancialYearData.LoadFinancialYearByDateTime(purchase.TransactionDateTime)).Id,
			VoucherId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseVoucherId)).Value),
			Remarks = purchase.Remarks,
			UserId = purchase.CreatedBy,
			GeneratedModule = GeneratedModules.Purchase.ToString(),
			CreatedAt = await CommonData.LoadCurrentDateTime(),
			Status = true
		});

		await SaveAccountingDetails(purchaseOverview, accountingId);
	}

	private static async Task SaveAccountingDetails(PurchaseOverviewModel purchaseOverview, int accountingId)
	{
		// Supplier Account Posting (Credit)
		if (purchaseOverview.TotalAmount > 0)
			await AccountingData.InsertAccountingDetails(new()
			{
				Id = 0,
				AccountingId = accountingId,
				LedgerId = purchaseOverview.PartyId,
				ReferenceId = purchaseOverview.Id,
				ReferenceType = ReferenceTypes.Purchase.ToString(),
				Debit = null,
				Credit = purchaseOverview.TotalAmount,
				Remarks = $"Party Account Posting For Purchase Bill {purchaseOverview.TransactionNo}",
				Status = true
			});

		// Purchase Account Posting (Debit)
		if (purchaseOverview.TotalAmount - purchaseOverview.TotalTaxAmount > 0)
			await AccountingData.InsertAccountingDetails(new()
			{
				Id = 0,
				AccountingId = accountingId,
				LedgerId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseLedgerId)).Value),
				ReferenceId = purchaseOverview.Id,
				ReferenceType = ReferenceTypes.Purchase.ToString(),
				Debit = purchaseOverview.TotalAmount - purchaseOverview.TotalTaxAmount,
				Credit = null,
				Remarks = $"Purchase Account Posting For Purchase Bill {purchaseOverview.TransactionNo}",
				Status = true
			});

		// GST Account Posting (Debit)
		if (purchaseOverview.TotalTaxAmount > 0)
			await AccountingData.InsertAccountingDetails(new()
			{
				Id = 0,
				AccountingId = accountingId,
				LedgerId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.GSTLedgerId)).Value),
				ReferenceId = purchaseOverview.Id,
				ReferenceType = ReferenceTypes.Purchase.ToString(),
				Debit = purchaseOverview.TotalTaxAmount,
				Credit = null,
				Remarks = $"GST Account Posting For Purchase Bill {purchaseOverview.TransactionNo}",
				Status = true
			});
	}

	private static async Task UpdateRawMaterialRateAndUOMOnPurchase(List<PurchaseItemCartModel> purchaseDetails)
	{
		var isUpdateItemRateOnPurchaseEnabled = bool.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.UpdateItemMasterRateOnPurchase)).Value);
		var isUpdateItemUOMOnPurchaseEnabled = bool.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.UpdateItemMasterUOMOnPurchase)).Value);

		var rawMaterials = await CommonData.LoadTableData<RawMaterialModel>(TableNames.RawMaterial);

		foreach (var purchaseItem in purchaseDetails)
		{
			var rawMaterial = rawMaterials.FirstOrDefault(i => i.Id == purchaseItem.ItemId);
			if (rawMaterial is not null)
			{
				if (isUpdateItemRateOnPurchaseEnabled)
					rawMaterial.Rate = purchaseItem.Rate;
				if (isUpdateItemUOMOnPurchaseEnabled)
					rawMaterial.UnitOfMeasurement = purchaseItem.UnitOfMeasurement;

				await RawMaterialData.InsertRawMaterial(rawMaterial);
			}
		}
	}
}