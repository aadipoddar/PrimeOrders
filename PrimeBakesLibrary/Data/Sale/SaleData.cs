using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.Data.Notification;
using PrimeBakesLibrary.Data.Order;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Stock;
using PrimeBakesLibrary.Models.Order;
using PrimeBakesLibrary.Models.Product;
using PrimeBakesLibrary.Models.Sale;

namespace PrimeBakesLibrary.Data.Sale;

public static class SaleData
{
	public static async Task<int> InsertSale(SaleModel sale) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertSale, sale)).FirstOrDefault();

	public static async Task InsertSaleDetail(SaleDetailModel saleDetail) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertSaleDetail, saleDetail);

	public static async Task<List<SaleDetailModel>> LoadSaleDetailBySale(int SaleId) =>
		await SqlDataAccess.LoadData<SaleDetailModel, dynamic>(StoredProcedureNames.LoadSaleDetailBySale, new { SaleId });

	public static async Task<List<SaleOverviewModel>> LoadSaleDetailsByDateLocationId(DateTime FromDate, DateTime ToDate, int LocationId) =>
		await SqlDataAccess.LoadData<SaleOverviewModel, dynamic>(StoredProcedureNames.LoadSaleDetailsByDateLocationId, new { FromDate, ToDate, LocationId });

	public static async Task<SaleModel> LoadLastSaleByLocation(int LocationId) =>
		(await SqlDataAccess.LoadData<SaleModel, dynamic>(StoredProcedureNames.LoadLastSaleByLocation, new { LocationId })).FirstOrDefault();

	public static async Task<SaleOverviewModel> LoadSaleOverviewBySaleId(int SaleId) =>
		(await SqlDataAccess.LoadData<SaleOverviewModel, dynamic>(StoredProcedureNames.LoadSaleOverviewBySaleId, new { SaleId })).FirstOrDefault();

	public static async Task<SaleModel> LoadSaleByBillNo(string BillNo) =>
		(await SqlDataAccess.LoadData<SaleModel, dynamic>(StoredProcedureNames.LoadSaleByBillNo, new { BillNo })).FirstOrDefault();

	public static async Task<int> SaveSale(SaleModel sale, List<SaleProductCartModel> cart)
	{
		bool update = sale.Id > 0;

		var user = await CommonData.LoadTableDataById<UserModel>(TableNames.User, sale.UserId);
		sale.LocationId = user.LocationId;
		sale.Status = true;
		sale.CreatedAt = DateTime.Now;
		sale.SaleDateTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateOnly.FromDateTime(sale.SaleDateTime)
			.ToDateTime(new TimeOnly(sale.SaleDateTime.Hour, sale.SaleDateTime.Minute, sale.SaleDateTime.Second)),
			"India Standard Time");
		sale.BillNo = update ? sale.BillNo : await GenerateCodes.GenerateSaleBillNo(sale);

		sale.Id = await InsertSale(sale);
		await SaveSaleDetail(sale, cart, update);
		await SaveStock(sale, cart, update);
		await UpdateOrder(sale, update);
		await InsertAccounting(sale, update);
		await SendNotification.SendSaleNotificationPartyAdmin(sale.Id);

		return sale.Id;
	}

	private static async Task SaveSaleDetail(SaleModel sale, List<SaleProductCartModel> cart, bool update)
	{
		if (update)
		{
			var existingSaleDetails = await LoadSaleDetailBySale(sale.Id);
			foreach (var item in existingSaleDetails)
			{
				item.Status = false;
				await InsertSaleDetail(item);
			}
		}

		foreach (var item in cart)
			await InsertSaleDetail(new()
			{
				Id = 0,
				SaleId = sale.Id,
				ProductId = item.ProductId,
				Quantity = item.Quantity,
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

	private static async Task SaveStock(SaleModel sale, List<SaleProductCartModel> cart, bool update)
	{
		if (update)
			await ProductStockData.DeleteProductStockByTransactionNo(sale.BillNo);

		foreach (var product in cart)
		{
			var item = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, product.ProductId);
			if (item.LocationId != 1)
				continue;

			await ProductStockData.InsertProductStock(new()
			{
				Id = 0,
				ProductId = product.ProductId,
				Quantity = -product.Quantity,
				NetRate = product.NetRate,
				TransactionNo = sale.BillNo,
				Type = StockType.Sale.ToString(),
				TransactionDate = DateOnly.FromDateTime(sale.SaleDateTime),
				LocationId = sale.LocationId
			});
		}

		if (sale.PartyId is null || sale.PartyId <= 0)
			return;

		var supplier = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, sale.PartyId.Value);
		if (supplier.LocationId.HasValue && supplier.LocationId.Value > 0)
			foreach (var product in cart)
				await ProductStockData.InsertProductStock(new()
				{
					Id = 0,
					ProductId = product.ProductId,
					Quantity = product.Quantity,
					NetRate = product.NetRate,
					Type = StockType.Purchase.ToString(),
					TransactionNo = sale.BillNo,
					TransactionDate = DateOnly.FromDateTime(sale.SaleDateTime),
					LocationId = supplier.LocationId.Value
				});
	}

	private static async Task UpdateOrder(SaleModel sale, bool update)
	{
		if (update)
		{
			var existingOrder = await OrderData.LoadOrderBySale(sale.Id);
			if (existingOrder is not null && existingOrder.Id > 0)
			{
				existingOrder.SaleId = null;
				await OrderData.InsertOrder(existingOrder);
			}
		}

		if (sale.OrderId is null)
			return;

		var order = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, sale.OrderId.Value);
		if (order is not null && order.Status)
		{
			order.SaleId = sale.Id;
			await OrderData.InsertOrder(order);
		}
	}

	private static async Task InsertAccounting(SaleModel sale, bool update)
	{
		if (sale.LocationId != 1)
			return;

		if (update)
		{
			var existingAccounting = await AccountingData.LoadAccountingByReferenceNo(sale.BillNo);
			if (existingAccounting is not null && existingAccounting.Id > 0)
			{
				existingAccounting.Status = false;
				await AccountingData.InsertAccounting(existingAccounting);
			}
		}

		int accountingId = await AccountingData.InsertAccounting(new()
		{
			Id = 0,
			ReferenceNo = sale.BillNo,
			AccountingDate = DateOnly.FromDateTime(sale.SaleDateTime),
			FinancialYearId = (await FinancialYearData.LoadFinancialYearByDate(DateOnly.FromDateTime(sale.SaleDateTime))).Id,
			VoucherId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.SalesVoucherId)).Value),
			Remarks = sale.Remarks,
			UserId = sale.UserId,
			GeneratedModule = GeneratedModules.Sales.ToString(),
			CreatedAt = DateTime.Now,
			Status = true
		});

		await InsertAccountingDetails(sale, accountingId);
	}

	private static async Task InsertAccountingDetails(SaleModel sale, int accountingId)
	{
		var saleOverview = await LoadSaleOverviewBySaleId(sale.Id);

		await AccountingData.InsertAccountingDetails(new()
		{
			Id = 0,
			AccountingId = accountingId,
			LedgerId = saleOverview.Credit > 0 ? saleOverview.PartyId.Value : int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.CashLedgerId)).Value),
			Debit = saleOverview.Cash + saleOverview.Card + saleOverview.UPI + saleOverview.Credit,
			Credit = null,
			Remarks = $"Cash / Party Account Posting For Sale Bill {saleOverview.BillNo}",
			Status = true
		});

		await AccountingData.InsertAccountingDetails(new()
		{
			Id = 0,
			AccountingId = accountingId,
			LedgerId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.SaleLedgerId)).Value),
			Debit = null,
			Credit = saleOverview.Cash + saleOverview.Card + saleOverview.UPI + saleOverview.Credit - saleOverview.TotalTaxAmount,
			Remarks = $"Sales Account Posting For Sale Bill {saleOverview.BillNo}",
			Status = true
		});

		await AccountingData.InsertAccountingDetails(new()
		{
			Id = 0,
			AccountingId = accountingId,
			LedgerId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.GSTLedgerId)).Value),
			Debit = null,
			Credit = saleOverview.TotalTaxAmount,
			Remarks = $"GST Account Posting For Sale Bill {saleOverview.BillNo}",
			Status = true
		});
	}
}
