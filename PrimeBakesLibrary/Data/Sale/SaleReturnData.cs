using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Stock;
using PrimeBakesLibrary.Models.Product;
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

	public static async Task<List<SaleReturnModel>> LoadSaleReturnBySale(int SaleId) =>
		await SqlDataAccess.LoadData<SaleReturnModel, dynamic>(StoredProcedureNames.LoadSaleReturnBySale, new { SaleId });

	public static async Task<List<SaleReturnOverviewModel>> LoadSaleReturnDetailsByDateLocationId(DateTime FromDate, DateTime ToDate, int LocationId) =>
		await SqlDataAccess.LoadData<SaleReturnOverviewModel, dynamic>(StoredProcedureNames.LoadSaleReturnDetailsByDateLocationId, new { FromDate, ToDate, LocationId });

	public static async Task<SaleReturnModel> LoadLastSaleReturnByLocation(int LocationId) =>
		(await SqlDataAccess.LoadData<SaleReturnModel, dynamic>(StoredProcedureNames.LoadLastSaleReturnByLocation, new { LocationId })).FirstOrDefault();

	public static async Task<SaleReturnOverviewModel> LoadSaleReturnOverviewBySaleReturnId(int SaleReturnId) =>
		(await SqlDataAccess.LoadData<SaleReturnOverviewModel, dynamic>(StoredProcedureNames.LoadSaleReturnOverviewBySaleReturnId, new { SaleReturnId })).FirstOrDefault();

	public static async Task<SaleReturnModel> LoadSaleReturnByTransactionNo(string TransactionNo) =>
		(await SqlDataAccess.LoadData<SaleReturnModel, dynamic>(StoredProcedureNames.LoadSaleReturnByTransactionNo, new { TransactionNo })).FirstOrDefault();

	public static async Task<int> SaveSaleReturn(SaleReturnModel saleReturn, List<SaleReturnProductCartModel> cart)
	{
		bool update = saleReturn.Id > 0;

		if (update)
			saleReturn.TransactionNo = (await CommonData.LoadTableDataById<SaleReturnModel>(TableNames.SaleReturn, saleReturn.Id)).TransactionNo;
		else
			saleReturn.TransactionNo = await GenerateCodes.GenerateSaleReturnTransactionNo(saleReturn);

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
			await ProductStockData.DeleteProductStockByTransactionNo(saleReturn.TransactionNo);

		var sale = await CommonData.LoadTableDataById<SaleModel>(TableNames.Sale, saleReturn.SaleId);
		LedgerModel party = null;
		if (sale.PartyId is not null && sale.PartyId > 1)
			party = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, sale.PartyId.Value);

		foreach (var product in cart)
		{
			var item = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, product.ProductId);
			if (item.LocationId != 1)
				continue;

			// Remove stock from the return location (negative quantity)
			if (sale.PartyId is not null && sale.PartyId > 1 && party is not null && party.LocationId > 1)
				await ProductStockData.InsertProductStock(new()
				{
					Id = 0,
					ProductId = product.ProductId,
					Quantity = -product.Quantity,
					TransactionNo = saleReturn.TransactionNo,
					Type = StockType.SaleReturn.ToString(),
					TransactionDate = DateOnly.FromDateTime(saleReturn.ReturnDateTime),
					LocationId = party.LocationId.Value
				});

			// Add stock back to Sale location (positive quantity)
			await ProductStockData.InsertProductStock(new()
			{
				Id = 0,
				ProductId = product.ProductId,
				Quantity = product.Quantity,
				TransactionNo = saleReturn.TransactionNo,
				Type = StockType.SaleReturn.ToString(),
				TransactionDate = DateOnly.FromDateTime(saleReturn.ReturnDateTime),
				LocationId = saleReturn.LocationId
			});
		}
	}

	private static async Task SaveAccounting(SaleReturnModel saleReturn, List<SaleReturnProductCartModel> cart, bool update)
	{
		if (update)
		{
			var existingAccounting = await AccountingData.LoadAccountingByTransactionNo(saleReturn.TransactionNo);
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
			TransactionNo = saleReturn.TransactionNo,
			AccountingDate = DateOnly.FromDateTime(saleReturn.ReturnDateTime),
			FinancialYearId = (await FinancialYearData.LoadFinancialYearByDate(DateOnly.FromDateTime(saleReturn.ReturnDateTime))).Id,
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
		if (saleReturnOverview.Total > 0)
			await AccountingData.InsertAccountingDetails(new()
			{
				Id = 0,
				AccountingId = accountingId,
				LedgerId = (await LedgerData.LoadLedgerByLocation(saleReturnOverview.LocationId)).Id,
				ReferenceId = saleReturnOverview.SaleReturnId,
				ReferenceType = ReferenceTypes.SaleReturn.ToString(),
				Credit = saleReturnOverview.Total,
				Debit = null,
				Remarks = $"Cash / Party Account Posting For Sale Return Bill {saleReturnOverview.TransactionNo}",
				Status = true
			});

		if (saleReturnOverview.Total - saleReturnOverview.TotalTaxAmount > 0)
			await AccountingData.InsertAccountingDetails(new()
			{
				Id = 0,
				AccountingId = accountingId,
				LedgerId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.SaleReturnVoucherId)).Value),
				ReferenceId = saleReturnOverview.SaleReturnId,
				ReferenceType = ReferenceTypes.SaleReturn.ToString(),
				Debit = saleReturnOverview.Total - saleReturnOverview.TotalTaxAmount,
				Credit = null,
				Remarks = $"Sales Return Account Posting For Sale Return Bill {saleReturnOverview.TransactionNo}",
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
				Remarks = $"GST Account Posting For Sale Return Bill {saleReturnOverview.TransactionNo}",
				Status = true
			});
	}
}
