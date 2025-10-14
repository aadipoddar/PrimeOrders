using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Stock;
using PrimeBakesLibrary.Models.Sale;

namespace PrimeBakesLibrary.Data.Sale;

public static class SaleReturnData
{
	public static async Task<int> InsertSaleReturn(SaleReturnModel saleReturn) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertSaleReturn, saleReturn)).FirstOrDefault();

	public static async Task InsertSaleReturnDetail(SaleReturnDetailModel saleReturnDetail) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertSaleReturnDetail, saleReturnDetail);

	public static async Task<List<SaleReturnDetailModel>> LoadSaleReturnDetailBySaleReturn(int SaleReturnId) =>
		await SqlDataAccess.LoadData<SaleReturnDetailModel, dynamic>(StoredProcedureNames.LoadSaleReturnDetailBySaleReturn, new { SaleReturnId });

	public static async Task<List<SaleReturnOverviewModel>> LoadSaleReturnDetailsByDateLocationId(DateTime FromDate, DateTime ToDate, int LocationId) =>
		await SqlDataAccess.LoadData<SaleReturnOverviewModel, dynamic>(StoredProcedureNames.LoadSaleReturnDetailsByDateLocationId, new { FromDate, ToDate, LocationId });

	public static async Task<SaleReturnModel> LoadLastSaleReturnByLocation(int LocationId) =>
		(await SqlDataAccess.LoadData<SaleReturnModel, dynamic>(StoredProcedureNames.LoadLastSaleReturnByLocation, new { LocationId })).FirstOrDefault();

	public static async Task<SaleReturnOverviewModel> LoadSaleReturnOverviewBySaleReturnId(int SaleReturnId) =>
		(await SqlDataAccess.LoadData<SaleReturnOverviewModel, dynamic>(StoredProcedureNames.LoadSaleReturnOverviewBySaleReturnId, new { SaleReturnId })).FirstOrDefault();

	public static async Task<SaleReturnModel> LoadSaleReturnByBillNo(string BillNo) =>
		(await SqlDataAccess.LoadData<SaleReturnModel, dynamic>(StoredProcedureNames.LoadSaleReturnByBillNo, new { BillNo })).FirstOrDefault();

	public static async Task<int> SaveSaleReturn(SaleReturnModel saleReturn, List<SaleReturnProductCartModel> cart)
	{
		bool update = saleReturn.Id > 0;

		saleReturn.Status = true;
		saleReturn.CreatedAt = DateTime.Now;
		saleReturn.LocationId = 1;
		saleReturn.SaleReturnDateTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateOnly.FromDateTime(saleReturn.SaleReturnDateTime)
			.ToDateTime(new TimeOnly(saleReturn.SaleReturnDateTime.Hour, saleReturn.SaleReturnDateTime.Minute, saleReturn.SaleReturnDateTime.Second)),
			"India Standard Time");

		if (update)
			saleReturn.BillNo = (await CommonData.LoadTableDataById<SaleReturnModel>(TableNames.SaleReturn, saleReturn.Id)).BillNo;
		else
			saleReturn.BillNo = await GenerateCodes.GenerateSaleReturnBillNo(saleReturn);

		saleReturn.Id = await InsertSaleReturn(saleReturn);
		await SaveSaleReturnDetail(saleReturn, cart, update);
		await SaveStock(saleReturn, cart, update);
		if (saleReturn.LocationId == 1)
			await SaveAccounting(saleReturn, cart, update);

		return saleReturn.Id;
	}

	private static async Task SaveSaleReturnDetail(SaleReturnModel saleReturn, List<SaleReturnProductCartModel> cart, bool update)
	{
		if (update)
		{
			var existingSaleReturnDetails = await LoadSaleReturnDetailBySaleReturn(saleReturn.Id);
			foreach (var existingDetail in existingSaleReturnDetails)
			{
				existingDetail.Status = false;
				await InsertSaleReturnDetail(existingDetail);
			}
		}

		foreach (var cartItem in cart)
			await InsertSaleReturnDetail(new()
			{
				Id = 0,
				SaleReturnId = saleReturn.Id,
				ProductId = cartItem.ProductId,
				Quantity = cartItem.Quantity,
				Rate = cartItem.Rate,
				BaseTotal = cartItem.BaseTotal,
				DiscPercent = cartItem.DiscPercent,
				DiscAmount = cartItem.DiscAmount,
				AfterDiscount = cartItem.AfterDiscount,
				CGSTPercent = cartItem.CGSTPercent,
				CGSTAmount = cartItem.CGSTAmount,
				SGSTPercent = cartItem.SGSTPercent,
				SGSTAmount = cartItem.SGSTAmount,
				IGSTPercent = cartItem.IGSTPercent,
				IGSTAmount = cartItem.IGSTAmount,
				NetRate = cartItem.NetRate,
				Total = cartItem.Total,
				Status = true
			});
	}

	private static async Task SaveStock(SaleReturnModel saleReturn, List<SaleReturnProductCartModel> cart, bool update)
	{
		if (update)
			await ProductStockData.DeleteProductStockByTransactionNo(saleReturn.BillNo);

		LedgerModel party = null;
		if (saleReturn.PartyId is not null && saleReturn.PartyId > 1)
			party = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, saleReturn.PartyId.Value);

		foreach (var product in cart)
		{
			// Remove stock from the return location (negative quantity)
			if (saleReturn.PartyId is not null && saleReturn.PartyId > 1 && party is not null && party.LocationId > 1)
				await ProductStockData.InsertProductStock(new()
				{
					Id = 0,
					ProductId = product.ProductId,
					Quantity = -product.Quantity,
					TransactionNo = saleReturn.BillNo,
					Type = StockType.SaleReturn.ToString(),
					TransactionDate = DateOnly.FromDateTime(saleReturn.SaleReturnDateTime),
					LocationId = party.LocationId.Value
				});

			// Add stock back to Sale location (positive quantity)
			await ProductStockData.InsertProductStock(new()
			{
				Id = 0,
				ProductId = product.ProductId,
				Quantity = product.Quantity,
				TransactionNo = saleReturn.BillNo,
				Type = StockType.SaleReturn.ToString(),
				TransactionDate = DateOnly.FromDateTime(saleReturn.SaleReturnDateTime),
				LocationId = saleReturn.LocationId
			});
		}
	}

	private static async Task SaveAccounting(SaleReturnModel saleReturn, List<SaleReturnProductCartModel> cart, bool update)
	{
		if (update)
		{
			var existingAccounting = await AccountingData.LoadAccountingByTransactionNo(saleReturn.BillNo);
			if (existingAccounting is not null && existingAccounting.Id > 0)
			{
				existingAccounting.Status = false;
				await AccountingData.InsertAccounting(existingAccounting);
			}
		}

		var saleReturnOverview = await LoadSaleReturnOverviewBySaleReturnId(saleReturn.Id);

		if (saleReturnOverview.Total <= 0 && saleReturnOverview.TotalTaxAmount <= 0)
			return;

		int accountingId = await AccountingData.InsertAccounting(new()
		{
			Id = 0,
			TransactionNo = saleReturn.BillNo,
			AccountingDate = DateOnly.FromDateTime(saleReturn.SaleReturnDateTime),
			FinancialYearId = (await FinancialYearData.LoadFinancialYearByDate(DateOnly.FromDateTime(saleReturn.SaleReturnDateTime))).Id,
			VoucherId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.SaleReturnVoucherId)).Value),
			Remarks = saleReturn.Remarks,
			UserId = saleReturn.UserId,
			GeneratedModule = GeneratedModules.SaleReturn.ToString(),
			CreatedAt = DateTime.Now,
			Status = true
		});

		await SaveAccountingDetails(saleReturnOverview, accountingId);
	}

	private static async Task SaveAccountingDetails(SaleReturnOverviewModel saleReturnOverview, int accountingId)
	{
		if (saleReturnOverview.Cash + saleReturnOverview.Card + saleReturnOverview.UPI + saleReturnOverview.Credit > 0)
			await AccountingData.InsertAccountingDetails(new()
			{
				Id = 0,
				AccountingId = accountingId,
				LedgerId = saleReturnOverview.Credit > 0 ? saleReturnOverview.PartyId.Value : int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.CashLedgerId)).Value),
				ReferenceId = saleReturnOverview.SaleReturnId,
				ReferenceType = ReferenceTypes.SaleReturn.ToString(),
				Debit = null,
				Credit = saleReturnOverview.Cash + saleReturnOverview.Card + saleReturnOverview.UPI + saleReturnOverview.Credit,
				Remarks = $"Cash / Party Account Posting For Sale Return Bill {saleReturnOverview.BillNo}",
				Status = true
			});

		if (saleReturnOverview.Cash + saleReturnOverview.Card + saleReturnOverview.UPI + saleReturnOverview.Credit - saleReturnOverview.TotalTaxAmount > 0)
			await AccountingData.InsertAccountingDetails(new()
			{
				Id = 0,
				AccountingId = accountingId,
				LedgerId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.SaleLedgerId)).Value),
				ReferenceId = saleReturnOverview.SaleReturnId,
				ReferenceType = ReferenceTypes.SaleReturn.ToString(),
				Debit = saleReturnOverview.Cash + saleReturnOverview.Card + saleReturnOverview.UPI + saleReturnOverview.Credit - saleReturnOverview.TotalTaxAmount,
				Credit = null,
				Remarks = $"Sales Return Account Posting For Sale Return Bill {saleReturnOverview.BillNo}",
				Status = true
			});

		if (saleReturnOverview.TotalTaxAmount > 0)
			await AccountingData.InsertAccountingDetails(new()
			{
				Id = 0,
				AccountingId = accountingId,
				LedgerId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.GSTLedgerId)).Value),
				ReferenceId = saleReturnOverview.SaleReturnId,
				ReferenceType = ReferenceTypes.SaleReturn.ToString(),
				Debit = saleReturnOverview.TotalTaxAmount,
				Credit = null,
				Remarks = $"GST Account Posting For Sale Return Bill {saleReturnOverview.BillNo}",
				Status = true
			});
	}
}
