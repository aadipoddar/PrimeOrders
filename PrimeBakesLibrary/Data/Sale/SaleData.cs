using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.Data.Notification;
using PrimeBakesLibrary.Data.Order;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Stock;
using PrimeBakesLibrary.Models.Order;
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

		sale.Status = true;
		sale.CreatedAt = DateTime.Now;
		sale.SaleDateTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateOnly.FromDateTime(sale.SaleDateTime)
			.ToDateTime(new TimeOnly(sale.SaleDateTime.Hour, sale.SaleDateTime.Minute, sale.SaleDateTime.Second)),
			"India Standard Time");

		var user = await CommonData.LoadTableDataById<UserModel>(TableNames.User, sale.UserId);

		if (update)
		{
			var existingSale = await CommonData.LoadTableDataById<SaleModel>(TableNames.Sale, sale.Id);
			sale.LocationId = existingSale.LocationId;
			sale.BillNo = existingSale.BillNo;
		}
		else
		{
			sale.LocationId = user.LocationId;
			sale.BillNo = await GenerateCodes.GenerateSaleBillNo(sale);
		}

		sale.Id = await InsertSale(sale);
		await SaveSaleDetail(sale, cart, update);
		await SaveStock(sale, cart, update);
		await SaveRawMaterialStockByRecipe(sale, cart);
		await UpdateOrder(sale, update);
		await SaveAccounting(sale, update);
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
		{
			await ProductStockData.DeleteProductStockByTransactionNo(sale.BillNo);
			await RawMaterialStockData.DeleteRawMaterialStockByTypeIdNo(StockType.Sale.ToString(), sale.Id, sale.BillNo);
		}

		foreach (var product in cart)
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

	private static async Task SaveRawMaterialStockByRecipe(SaleModel sale, List<SaleProductCartModel> cart)
	{
		if (sale.LocationId != 1)
			return;

		foreach (var product in cart)
		{
			var recipe = await RecipeData.LoadRecipeByProduct(product.ProductId);
			var recipeItems = recipe is null ? [] : await RecipeData.LoadRecipeDetailByRecipe(recipe.Id);

			foreach (var recipeItem in recipeItems)
				await RawMaterialStockData.InsertRawMaterialStock(new()
				{
					Id = 0,
					RawMaterialId = recipeItem.RawMaterialId,
					Quantity = -recipeItem.Quantity * product.Quantity,
					NetRate = product.NetRate / recipeItem.Quantity,
					TransactionId = sale.Id,
					TransactionNo = sale.BillNo,
					Type = StockType.Sale.ToString(),
					TransactionDate = DateOnly.FromDateTime(sale.SaleDateTime)
				});
		}
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

	private static async Task SaveAccounting(SaleModel sale, bool update)
	{
		if (sale.LocationId != 1)
			return;

		if (update)
		{
			var existingAccounting = await AccountingData.LoadAccountingByTransactionNo(sale.BillNo);
			if (existingAccounting is not null && existingAccounting.Id > 0)
			{
				existingAccounting.Status = false;
				await AccountingData.InsertAccounting(existingAccounting);
			}
		}

		var saleOverview = await LoadSaleOverviewBySaleId(sale.Id);

		if ((saleOverview.Cash + saleOverview.Card + saleOverview.UPI + saleOverview.Credit) <= 0 && saleOverview.TotalTaxAmount <= 0)
			return;

		int accountingId = await AccountingData.InsertAccounting(new()
		{
			Id = 0,
			TransactionNo = sale.BillNo,
			AccountingDate = DateOnly.FromDateTime(sale.SaleDateTime),
			FinancialYearId = (await FinancialYearData.LoadFinancialYearByDateTime(sale.SaleDateTime)).Id,
			VoucherId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.SalesVoucherId)).Value),
			Remarks = sale.Remarks,
			UserId = sale.UserId,
			GeneratedModule = GeneratedModules.Sales.ToString(),
			CreatedAt = DateTime.Now,
			Status = true
		});

		await SaveAccountingDetails(saleOverview, accountingId);
	}

	private static async Task SaveAccountingDetails(SaleOverviewModel saleOverview, int accountingId)
	{
		if (saleOverview.Cash + saleOverview.Card + saleOverview.UPI + saleOverview.Credit > 0)
			await AccountingData.InsertAccountingDetails(new()
			{
				Id = 0,
				AccountingId = accountingId,
				LedgerId = saleOverview.Credit > 0 ? saleOverview.PartyId.Value : int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.CashLedgerId)).Value),
				ReferenceId = saleOverview.SaleId,
				ReferenceType = ReferenceTypes.Sales.ToString(),
				Debit = saleOverview.Cash + saleOverview.Card + saleOverview.UPI + saleOverview.Credit,
				Credit = null,
				Remarks = $"Cash / Party Account Posting For Sale Bill {saleOverview.BillNo}",
				Status = true
			});

		if (saleOverview.Cash + saleOverview.Card + saleOverview.UPI + saleOverview.Credit - saleOverview.TotalTaxAmount > 0)
			await AccountingData.InsertAccountingDetails(new()
			{
				Id = 0,
				AccountingId = accountingId,
				LedgerId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.SaleLedgerId)).Value),
				ReferenceId = saleOverview.SaleId,
				ReferenceType = ReferenceTypes.Sales.ToString(),
				Debit = null,
				Credit = saleOverview.Cash + saleOverview.Card + saleOverview.UPI + saleOverview.Credit - saleOverview.TotalTaxAmount,
				Remarks = $"Sales Account Posting For Sale Bill {saleOverview.BillNo}",
				Status = true
			});

		if (saleOverview.TotalTaxAmount > 0)
			await AccountingData.InsertAccountingDetails(new()
			{
				Id = 0,
				AccountingId = accountingId,
				LedgerId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.GSTLedgerId)).Value),
				ReferenceId = saleOverview.SaleId,
				ReferenceType = ReferenceTypes.Sales.ToString(),
				Debit = null,
				Credit = saleOverview.TotalTaxAmount,
				Remarks = $"GST Account Posting For Sale Bill {saleOverview.BillNo}",
				Status = true
			});
	}

	public static async Task DeleteSale(SaleModel sale)
	{
		sale.Status = false;
		await InsertSale(sale);
		await ProductStockData.DeleteProductStockByTransactionNo(sale.BillNo);
		await RawMaterialStockData.DeleteRawMaterialStockByTypeIdNo(StockType.Sale.ToString(), sale.Id, sale.BillNo);

		if (sale.LocationId != 1)
			return;

		var accounting = await AccountingData.LoadAccountingByTransactionNo(sale.BillNo);
		if (accounting is null)
			return;

		accounting.Status = false;
		await AccountingData.InsertAccounting(accounting);
	}
}
