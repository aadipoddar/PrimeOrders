using OfficeOpenXml;

using PrimeBakesLibrary.Data;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Kitchen;
using PrimeBakesLibrary.Data.Inventory.Purchase;
using PrimeBakesLibrary.Data.Sales.Order;
using PrimeBakesLibrary.Data.Sales.Sale;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Inventory.Purchase;
using PrimeBakesLibrary.Models.Sales.Order;
using PrimeBakesLibrary.Models.Sales.Sale;

//FileInfo fileInfo = new(@"C:\Others\order.xlsx");

//ExcelPackage.License.SetNonCommercialPersonal("AadiSoft");

//using var package = new ExcelPackage(fileInfo);

//await package.LoadAsync(fileInfo);

//var worksheet1 = package.Workbook.Worksheets[0];
//var worksheet2 = package.Workbook.Worksheets[1];

// await InsertProducts(worksheet);

// await UpdateProducts();

// await InsertRawMaterial(worksheet);

// await InsertSupplier(worksheet);

// await InsertProductLocations();

// await InsertFixProductLocations(worksheet);

// await InsertAccounting(worksheet);

// await InsertAccountingDetails(worksheet);

// await RecalculateBills();

// await RecalculateReturnBills();

// await UpdateProductLocation();

// await InsertPurchase(worksheet1, worksheet2);

// await UpdateRMStockPurchaseIssue();

//await UpdateProductStockSaleAndReturn();

// await InsertIssues(worksheet1, worksheet2);

// await InsertReturns(worksheet1, worksheet2);

// await InsertSales(worksheet1, worksheet2);

// await InsertOrder(worksheet1, worksheet2);

// await FixMasterNames();

// await FixLedgers();

await RecalculateTransactions();

// await UpdateAccounts();

Console.WriteLine("Finished importing Items.");
Console.ReadLine();

#region UnusedMethods

//static async Task UpdateProducts()
//{
//	var products = await CommonData.LoadTableData<ProductModel>(TableNames.Product);
//	int row = 1;

//	foreach (var product in products)
//	{
//		Console.WriteLine(product.Name);

//		var code = "FP" + row.ToString("D4");

//		await ProductData.InsertProduct(new()
//		{
//			Id = product.Id,
//			Code = code,
//			Name = product.Name,
//			ProductCategoryId = product.ProductCategoryId,
//			Rate = product.Rate,
//			TaxId = product.TaxId,
//			Status = product.Status
//		});

//		Console.WriteLine("Updated Product: " + product.Name + " with code " + code);
//		row++;
//	}
//}

//static async Task InsertRawMaterial(ExcelWorksheet worksheet)
//{
//	int row = 1;

//	while (worksheet.Cells[row, 2].Value != null)
//	{
//		var name = worksheet.Cells[row, 2].Value.ToString();
//		var unit = worksheet.Cells[row, 3].Value?.ToString();
//		var code = "RM" + row.ToString("D4");

//		if (string.IsNullOrWhiteSpace(unit))
//			unit = "KG";

//		if (string.IsNullOrWhiteSpace(name))
//		{
//			Console.WriteLine("Not Inserted Row = " + row);
//			continue;
//		}

//		unit = unit.Replace(" ", "");

//		Console.WriteLine("Inserting New Raw Material: " + name + " with unit " + unit);
//		await RawMaterialData.InsertRawMaterial(new()
//		{
//			Id = row,
//			Code = code,
//			Name = name,
//			Rate = 0,
//			RawMaterialCategoryId = 1,
//			UnitOfMeasurement = unit,
//			TaxId = 7,
//			Status = true
//		});

//		row++;
//	}
//}

//static async Task InsertProducts(ExcelWorksheet worksheet)
//{
//	int row = 1;

//	while (worksheet.Cells[row, 1].Value != null)
//	{
//		var code = "FP" + row.ToString("D4");
//		var name = worksheet.Cells[row, 1].Value.ToString();
//		var categoryId = worksheet.Cells[row, 2].Value.ToString();
//		var taxId = 7;
//		var price = 0;

//		if (string.IsNullOrWhiteSpace(code) ||
//			string.IsNullOrWhiteSpace(name) ||
//			string.IsNullOrWhiteSpace(categoryId))
//		{
//			Console.WriteLine("Not Inserted Row = " + row);
//			continue;
//		}

//		code = code.Replace(" ", "");

//		Console.WriteLine("Inserting New Product: " + name + " and code " + code);
//		await ProductData.InsertProduct(new()
//		{
//			Id = 0,
//			Code = code,
//			Name = name,
//			ProductCategoryId = int.Parse(categoryId),
//			Rate = price,
//			TaxId = taxId,
//			Status = true
//		});

//		row++;
//	}
//}

//static async Task InsertSupplier(ExcelWorksheet worksheet)
//{
//	int row = 2;

//	while (worksheet.Cells[row, 1].Value != null)
//	{
//		var code = "LD" + (row - 1).ToString("D5");
//		var name = worksheet.Cells[row, 1].Value.ToString();
//		var address = worksheet.Cells[row, 2].Value?.ToString() ?? string.Empty;
//		var remarks = worksheet.Cells[row, 3].Value?.ToString() ?? string.Empty;
//		var accountType = worksheet.Cells[row, 4].Value?.ToString() ?? string.Empty;
//		var phone = worksheet.Cells[row, 5].Value?.ToString() ?? string.Empty;
//		var gstNo = worksheet.Cells[row, 6].Value?.ToString() ?? string.Empty;

//		if (string.IsNullOrWhiteSpace(name))
//		{
//			Console.WriteLine("Not Inserted Row = " + row);
//			continue;
//		}

//		Console.WriteLine("Inserting Ledger: " + name + " and code " + code);
//		await LedgerData.InsertLedger(new()
//		{
//			Id = 0,
//			Code = code,
//			Name = name,
//			Address = address ?? string.Empty,
//			GSTNo = gstNo ?? string.Empty,
//			Phone = phone ?? string.Empty,
//			StateUTId = 1,
//			LocationId = null,
//			Remarks = remarks ?? string.Empty,
//			AccountTypeId = int.Parse(accountType),
//			GroupId = 1,
//			Status = true
//		});

//		row++;
//	}
//}

//static async Task InsertProductLocations()
//{
//	var products = await CommonData.LoadTableData<ProductModel>(TableNames.Product);
//	var locations = await CommonData.LoadTableData<LocationModel>(TableNames.Location);

//	foreach (var product in products)
//		foreach (var location in locations)
//			await ProductData.InsertProductLocation(new()
//			{
//				Id = 0,
//				ProductId = product.Id,
//				LocationId = location.Id,
//				Rate = product.Rate,
//				Status = true
//			});
//}

//static async Task InsertFixProductLocations(ExcelWorksheet worksheet)
//{
//	int row = 2;
//	while (worksheet.Cells[row, 1].Value != null)
//	{
//		var productId = worksheet.Cells[row, 1].Value.ToString();
//		var rate = worksheet.Cells[row, 2].Value.ToString();
//		var locationId = worksheet.Cells[row, 3].Value.ToString();

//		if (string.IsNullOrWhiteSpace(productId) ||
//			string.IsNullOrWhiteSpace(locationId) ||
//			string.IsNullOrWhiteSpace(rate))
//		{
//			Console.WriteLine("Not Inserted Row = " + row);
//			continue;
//		}

//		Console.WriteLine("Inserting Product Location for ProductId: " + productId + " and LocationId " + locationId);

//		var product = (await ProductData.LoadProductRateByProduct(int.Parse(productId))).Where(p => p.LocationId == int.Parse(locationId)).FirstOrDefault();
//		if (product is null)
//		{
//			Console.WriteLine("Product Not Found for ProductId: " + productId + " and LocationId " + locationId);
//			row++;
//			continue;
//		}

//		await ProductData.InsertProductLocation(new()
//		{
//			Id = product.Id,
//			ProductId = int.Parse(productId),
//			LocationId = int.Parse(locationId),
//			Rate = decimal.Parse(rate),
//			Status = true
//		});
//		row++;
//	}
//}

//static async Task InsertAccounting(ExcelWorksheet worksheet)
//{
//	Dapper.SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
//	int row = 2;
//	while (worksheet.Cells[row, 1].Value != null)
//	{
//		var referenceNo = worksheet.Cells[row, 2].Value.ToString();
//		var voucherId = worksheet.Cells[row, 3].Value.ToString();
//		var remarks = worksheet.Cells[row, 4].Value?.ToString() ?? string.Empty;
//		var date = worksheet.Cells[row, 5].Value.ToString();
//		var financialYearId = worksheet.Cells[row, 6].Value.ToString();
//		var userId = worksheet.Cells[row, 7].Value.ToString();
//		var generatedMode = worksheet.Cells[row, 8].Value.ToString();
//		var createdAt = worksheet.Cells[row, 9].Value.ToString();
//		var status = worksheet.Cells[row, 10].Value.ToString();

//		Console.WriteLine("Inserting Accounting Voucher: " + referenceNo + " and voucherId " + voucherId);
//		await AccountingData.InsertAccounting(new()
//		{
//			Id = 0,
//			TransactionNo = referenceNo,
//			UserId = int.Parse(userId),
//			VoucherId = int.Parse(voucherId),
//			Remarks = remarks,
//			FinancialYearId = int.Parse(financialYearId),
//			GeneratedModule = generatedMode,
//			AccountingDate = DateOnly.FromDateTime(DateTime.Parse(date)),
//			CreatedAt = DateTime.Now,
//			Status = status.ToLower() == "true",
//		});

//		row++;
//	}
//}

//static async Task InsertAccountingDetails(ExcelWorksheet worksheet)
//{
//	Dapper.SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
//	int row = 2;
//	while (worksheet.Cells[row, 1].Value != null)
//	{
//		var accountingId = worksheet.Cells[row, 2].Value?.ToString();
//		var ledgerId = worksheet.Cells[row, 3].Value?.ToString();
//		var debit = worksheet.Cells[row, 4].Value?.ToString();
//		var credit = worksheet.Cells[row, 5].Value?.ToString();
//		var remarks = worksheet.Cells[row, 6].Value?.ToString() ?? string.Empty;
//		var status = worksheet.Cells[row, 7].Value?.ToString();

//		// Skip row if essential fields are null or empty
//		if (string.IsNullOrWhiteSpace(accountingId) || string.IsNullOrWhiteSpace(ledgerId) || string.IsNullOrWhiteSpace(status))
//		{
//			Console.WriteLine("Skipping row " + row + " due to missing required fields");
//			row++;
//			continue;
//		}

//		var accounting = await CommonData.LoadTableDataById<AccountingModel>(TableNames.Accounting, int.Parse(accountingId));

//		int? id = null;
//		if (accounting.GeneratedModule == "Sales")
//		{
//			var sale = await SaleData.LoadSaleByBillNo(accounting.TransactionNo);
//			id = sale?.Id;
//		}
//		else if (accounting.GeneratedModule == "SaleReturn")
//		{
//			var saleReturn = await SaleReturnData.LoadSaleReturnByBillNo(accounting.TransactionNo);
//			id = saleReturn?.Id;
//		}
//		else if (accounting.GeneratedModule == "Purchase")
//			id = (await CommonData.LoadTableData<PurchaseModelOld>(TableNames.Purchase)).Where(p => p.BillNo == accounting.TransactionNo).FirstOrDefault()?.Id ?? 0;

//		Console.WriteLine("Inserting Accounting Voucher: " + accounting.TransactionNo + " and voucherId " + accounting.VoucherId);

//		// Helper method to safely parse decimal values
//		decimal? ParseNullableDecimal(string? value)
//		{
//			if (string.IsNullOrWhiteSpace(value) || value.Equals("NULL", StringComparison.OrdinalIgnoreCase))
//				return null;
//			return decimal.TryParse(value, out var result) ? result : null;
//		}

//		await AccountingData.InsertAccountingDetails(new()
//		{
//			Id = 0,
//			AccountingId = int.Parse(accountingId),
//			LedgerId = int.Parse(ledgerId),
//			ReferenceId = id,
//			ReferenceType = id is not null ? accounting.GeneratedModule : null,
//			Credit = ParseNullableDecimal(credit),
//			Debit = ParseNullableDecimal(debit),
//			Remarks = remarks,
//			Status = status.Equals("true", StringComparison.OrdinalIgnoreCase),
//		});

//		row++;
//	}
//}

//static async Task RecalculateBills()
//{
//	var allSales = await CommonData.LoadTableDataByStatus<SaleModel>(TableNames.Sale);
//	foreach (var sale in allSales)
//	{
//		var saleDetails = await SaleData.LoadSaleDetailBySale(sale.Id);

//		Console.WriteLine("Recalculating Bill No: " + sale.BillNo);

//		foreach (var item in saleDetails)
//		{
//			var baseTotal = item.Quantity * item.Rate;
//			var cgstAmount = baseTotal * item.CGSTPercent / 100;
//			var sgstAmount = baseTotal * item.SGSTPercent / 100;
//			var igstAmount = baseTotal * item.IGSTPercent / 100;
//			var total = baseTotal + cgstAmount + sgstAmount + igstAmount;
//			// Add 0 Check
//			decimal netRate = 0;
//			if (item.Quantity > 0)
//				netRate = total / item.Quantity * (1 - (sale.DiscPercent / 100));

//			await SaleData.InsertSaleDetail(new()
//			{
//				Id = item.Id,
//				SaleId = item.SaleId,
//				ProductId = item.ProductId,
//				Quantity = item.Quantity,
//				Rate = item.Rate,
//				BaseTotal = baseTotal,
//				DiscPercent = 0,
//				DiscAmount = 0,
//				AfterDiscount = baseTotal,
//				CGSTPercent = item.CGSTPercent,
//				CGSTAmount = cgstAmount,
//				SGSTPercent = item.SGSTPercent,
//				SGSTAmount = sgstAmount,
//				IGSTPercent = item.IGSTPercent,
//				IGSTAmount = igstAmount,
//				Total = total,
//				NetRate = netRate,
//				Status = true
//			});
//		}

//		var newSaleDetails = await SaleData.LoadSaleDetailBySale(sale.Id);
//		var newTotal = newSaleDetails.Sum(d => d.Total) * (1 - (sale.DiscPercent / 100));
//		sale.RoundOff = Math.Round(newTotal) - newTotal;

//		if (sale.Cash > 0)
//			sale.Cash = Math.Round(newTotal);
//		if (sale.Card > 0)
//			sale.Card = Math.Round(newTotal);
//		if (sale.UPI > 0)
//			sale.UPI = Math.Round(newTotal);
//		if (sale.Credit > 0)
//			sale.Credit = Math.Round(newTotal);

//		await SaleData.InsertSale(sale);
//	}
//}

//static async Task RecalculateReturnBills()
//{
//	var allSales = await CommonData.LoadTableDataByStatus<SaleReturnModel>(TableNames.SaleReturn);
//	foreach (var sale in allSales)
//	{
//		var saleDetails = await SaleReturnData.LoadSaleReturnDetailBySaleReturn(sale.Id);

//		Console.WriteLine("Recalculating Bill No: " + sale.BillNo);

//		foreach (var item in saleDetails)
//		{
//			var baseTotal = item.Quantity * item.Rate;
//			var cgstAmount = baseTotal * item.CGSTPercent / 100;
//			var sgstAmount = baseTotal * item.SGSTPercent / 100;
//			var igstAmount = baseTotal * item.IGSTPercent / 100;
//			var total = baseTotal + cgstAmount + sgstAmount + igstAmount;
//			// Add 0 Check
//			decimal netRate = 0;
//			if (item.Quantity > 0)
//				netRate = total / item.Quantity * (1 - (sale.DiscPercent / 100));

//			await SaleReturnData.InsertSaleReturnDetail(new()
//			{
//				Id = item.Id,
//				SaleReturnId = item.SaleReturnId,
//				ProductId = item.ProductId,
//				Quantity = item.Quantity,
//				Rate = item.Rate,
//				BaseTotal = baseTotal,
//				DiscPercent = 0,
//				DiscAmount = 0,
//				AfterDiscount = baseTotal,
//				CGSTPercent = item.CGSTPercent,
//				CGSTAmount = cgstAmount,
//				SGSTPercent = item.SGSTPercent,
//				SGSTAmount = sgstAmount,
//				IGSTPercent = item.IGSTPercent,
//				IGSTAmount = igstAmount,
//				Total = total,
//				NetRate = netRate,
//				Status = true
//			});
//		}

//		var newSaleDetails = await SaleReturnData.LoadSaleReturnDetailBySaleReturn(sale.Id);
//		var newTotal = newSaleDetails.Sum(d => d.Total) * (1 - (sale.DiscPercent / 100));
//		sale.RoundOff = Math.Round(newTotal) - newTotal;

//		if (sale.Cash > 0)
//			sale.Cash = Math.Round(newTotal);
//		if (sale.Card > 0)
//			sale.Card = Math.Round(newTotal);
//		if (sale.UPI > 0)
//			sale.UPI = Math.Round(newTotal);
//		if (sale.Credit > 0)
//			sale.Credit = Math.Round(newTotal);

//		await SaleReturnData.InsertSaleReturn(sale);
//	}
//}

//static async Task UpdateProductLocation()
//{
//	var products = await CommonData.LoadTableDataByStatus<ProductModel>(TableNames.Product);
//	foreach (var product in products)
//		await ProductData.InsertProductLocation(new()
//		{
//			Id = 0,
//			ProductId = product.Id,
//			LocationId = 48,
//			Rate = product.Rate,
//			Status = true
//		});
//}

//static async Task InsertPurchase(ExcelWorksheet worksheet1, ExcelWorksheet worksheet2)
//{
//	Dapper.SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

//	var row = 2;

//	List<PurchaseModelOld> purchase = [];
//	List<PurchaseDetailModelOld> purchaseDetails = [];

//	while (worksheet1.Cells[row, 1].Value != null)
//	{
//		var id = worksheet1.Cells[row, 1].Value.ToString();
//		var billNo = worksheet1.Cells[row, 2].Value.ToString();
//		var supplierId = worksheet1.Cells[row, 3].Value.ToString();
//		var billDateTime = worksheet1.Cells[row, 4].Value.ToString();
//		var cdPercent = worksheet1.Cells[row, 5].Value.ToString();
//		var roundOff = worksheet1.Cells[row, 6].Value.ToString();
//		var remarks = worksheet1.Cells[row, 7].Value?.ToString() ?? string.Empty;
//		var userId = worksheet1.Cells[row, 8].Value.ToString();
//		var createdAt = worksheet1.Cells[row, 9].Value.ToString();
//		var status = worksheet1.Cells[row, 10].Value.ToString();

//		purchase.Add(new()
//		{
//			Id = int.Parse(id),
//			BillNo = billNo,
//			SupplierId = int.Parse(supplierId),
//			BillDateTime = DateTime.Parse(billDateTime),
//			CDPercent = decimal.Parse(cdPercent),
//			RoundOff = decimal.Parse(roundOff),
//			Remarks = remarks == "NULL" ? string.Empty : remarks,
//			UserId = int.Parse(userId),
//			CreatedAt = DateTime.Parse(createdAt),
//			Status = status.ToLower() == "true",
//		});

//		row++;
//	}

//	purchase.RemoveAll(_ => _.Status == false);

//	row = 2;
//	while (worksheet2.Cells[row, 1].Value != null)
//	{
//		var id = worksheet2.Cells[row, 1].Value.ToString();
//		var purchaseId = worksheet2.Cells[row, 2].Value.ToString();
//		var rawMaterialId = worksheet2.Cells[row, 3].Value.ToString();
//		var quantity = worksheet2.Cells[row, 4].Value.ToString();
//		var measurementUnit = worksheet2.Cells[row, 5].Value.ToString();
//		var rate = worksheet2.Cells[row, 6].Value.ToString();
//		var baseTotal = worksheet2.Cells[row, 7].Value.ToString();
//		var discPercent = worksheet2.Cells[row, 8].Value.ToString();
//		var discAmount = worksheet2.Cells[row, 9].Value.ToString();
//		var afterDiscount = worksheet2.Cells[row, 10].Value.ToString();
//		var cgstPercent = worksheet2.Cells[row, 11].Value.ToString();
//		var cgstAmount = worksheet2.Cells[row, 12].Value.ToString();
//		var sgstPercent = worksheet2.Cells[row, 13].Value.ToString();
//		var sgstAmount = worksheet2.Cells[row, 14].Value.ToString();
//		var igstPercent = worksheet2.Cells[row, 15].Value.ToString();
//		var igstAmount = worksheet2.Cells[row, 16].Value.ToString();
//		var total = worksheet2.Cells[row, 17].Value.ToString();
//		var netRate = worksheet2.Cells[row, 18].Value.ToString();
//		var status = worksheet2.Cells[row, 19].Value.ToString();

//		purchaseDetails.Add(new()
//		{
//			Id = int.Parse(id),
//			PurchaseId = int.Parse(purchaseId),
//			RawMaterialId = int.Parse(rawMaterialId),
//			Quantity = decimal.Parse(quantity),
//			MeasurementUnit = measurementUnit,
//			Rate = decimal.Parse(rate),
//			BaseTotal = decimal.Parse(baseTotal),
//			DiscPercent = decimal.Parse(discPercent),
//			DiscAmount = decimal.Parse(discAmount),
//			AfterDiscount = decimal.Parse(afterDiscount),
//			CGSTPercent = decimal.Parse(cgstPercent),
//			CGSTAmount = decimal.Parse(cgstAmount),
//			SGSTPercent = decimal.Parse(sgstPercent),
//			SGSTAmount = decimal.Parse(sgstAmount),
//			IGSTPercent = decimal.Parse(igstPercent),
//			IGSTAmount = decimal.Parse(igstAmount),
//			Total = decimal.Parse(total),
//			NetRate = decimal.Parse(netRate),
//			Status = status.ToLower() == "1",
//		});

//		row++;
//	}

//	purchaseDetails.RemoveAll(_ => _.Status == false);

//	Console.WriteLine("Loaded " + purchase.Count + " purchases.");
//	Console.WriteLine("Loaded " + purchaseDetails.Count + " purchase details.");

//	foreach (var pur in purchase)
//	{
//		Console.WriteLine("Inserting Purchase Bill Id: " + pur.Id);

//		var purchaseDetail = purchaseDetails.Where(pd => pd.PurchaseId == pur.Id).ToList();
//		if (purchaseDetail.Count == 0)
//		{
//			Console.WriteLine("No Purchase Details Found for Purchase Id: " + pur.Id);
//			continue;
//		}
//		List<PurchaseItemCartModel> purchaseCart = [];

//		foreach (var detail in purchaseDetail)
//			purchaseCart.Add(new()
//			{
//				ItemId = detail.RawMaterialId,
//				ItemName = "",
//				UnitOfMeasurement = detail.MeasurementUnit,
//				Remarks = null,
//				InclusiveTax = false,
//				Quantity = detail.Quantity,
//				Rate = detail.Rate,
//				DiscountPercent = detail.DiscPercent,
//				CGSTPercent = detail.CGSTPercent,
//				SGSTPercent = detail.SGSTPercent,
//				IGSTPercent = detail.IGSTPercent,
//			});

//		foreach (var item in purchaseCart)
//		{
//			item.BaseTotal = item.Rate * item.Quantity;
//			item.DiscountAmount = item.BaseTotal * (item.DiscountPercent / 100);
//			item.AfterDiscount = item.BaseTotal - item.DiscountAmount;
//			item.CGSTAmount = item.AfterDiscount * (item.CGSTPercent / 100);
//			item.SGSTAmount = item.AfterDiscount * (item.SGSTPercent / 100);
//			item.IGSTAmount = item.AfterDiscount * (item.IGSTPercent / 100);
//			item.TotalTaxAmount = item.CGSTAmount + item.SGSTAmount + item.IGSTAmount;
//			item.Total = item.AfterDiscount + item.TotalTaxAmount;
//			var perUnitCost = item.Total / item.Quantity;
//			var withOtherCharges = perUnitCost * (1 + 0 / 100);
//			item.NetRate = withOtherCharges * (1 - pur.CDPercent / 100);
//		}

//		PurchaseModel finalPurchase = new()
//		{
//			Id = 0,
//			TransactionNo = pur.BillNo,
//			PartyId = pur.SupplierId,
//			TransactionDateTime = pur.BillDateTime,
//			RoundOffAmount = pur.RoundOff,
//			Remarks = string.IsNullOrWhiteSpace(pur.Remarks) ? null : pur.Remarks,
//			CreatedBy = pur.UserId,
//			OtherChargesPercent = 0,
//			OtherChargesAmount = 0,
//			DocumentUrl = null,
//			CompanyId = 1,
//			FinancialYearId = 1,
//			CreatedFromPlatform = "ImportScript",
//			CreatedAt = pur.CreatedAt,
//			Status = true,

//			ItemsTotalAmount = purchaseCart.Sum(x => x.Total),
//			CashDiscountPercent = pur.CDPercent
//		};
//		finalPurchase.CashDiscountAmount = finalPurchase.ItemsTotalAmount * (pur.CDPercent / 100);
//		finalPurchase.TotalAmount = finalPurchase.ItemsTotalAmount - finalPurchase.CashDiscountAmount + finalPurchase.RoundOffAmount;

//		await PurchaseData.SavePurchaseTransaction(finalPurchase, purchaseCart);
//		Console.WriteLine("Inserted Purchase Bill Id: " + pur.Id);
//	}
//}

//static async Task UpdateRMStockPurchaseIssue()
//{
//	Dapper.SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

//	var purchases = await CommonData.LoadTableDataByStatus<PurchaseOverviewModel>(ViewNames.PurchaseOverview);
//	foreach (var purchase in purchases)
//	{
//		Console.WriteLine("Updating RM Stock Purchase Issue for Purchase Id: " + purchase.Id);

//		var purchaseDetails = await PurchaseData.LoadPurchaseDetailByPurchase(purchase.Id);

//		foreach (var detail in purchaseDetails)
//		{
//			Console.WriteLine("Inserting RM Stock for Raw Material Id: " + detail.RawMaterialId + " Quantity: " + detail.Quantity);

//			await RawMaterialStockData.InsertRawMaterialStock(new()
//			{
//				Id = 0,
//				RawMaterialId = detail.RawMaterialId,
//				Quantity = detail.Quantity,
//				Type = StockType.Purchase.ToString(),
//				TransactionId = purchase.Id,
//				NetRate = detail.NetRate,
//				TransactionDate = DateOnly.FromDateTime(purchase.TransactionDateTime),
//				TransactionNo = purchase.TransactionNo,
//			});
//		}
//	}

//	var issues = await CommonData.LoadTableDataByStatus<KitchenIssueModel>(TableNames.KitchenIssue);
//	foreach (var issue in issues)
//	{
//		Console.WriteLine("Updating RM Stock Kitchen Issue for Kitchen Issue Id: " + issue.Id);

//		var issueDetails = await KitchenIssueData.LoadKitchenIssueDetailByKitchenIssue(issue.Id);

//		foreach (var detail in issueDetails)
//		{
//			Console.WriteLine("Inserting RM Stock for Raw Material Id: " + detail.RawMaterialId + " Quantity: " + detail.Quantity);
//			await RawMaterialStockData.InsertRawMaterialStock(new()
//			{
//				Id = 0,
//				RawMaterialId = detail.RawMaterialId,
//				Quantity = -detail.Quantity,
//				Type = StockType.KitchenIssue.ToString(),
//				TransactionId = issue.Id,
//				NetRate = null,
//				TransactionDate = DateOnly.FromDateTime(DateTime.Now),
//				TransactionNo = issue.TransactionNo,
//			});
//		}
//	}
//}

//static async Task UpdateProductStockSaleAndReturn()
//{
//	Dapper.SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

//	var sales = await CommonData.LoadTableDataByStatus<SaleModel>(TableNames.Sale);
//	foreach (var sale in sales)
//	{
//		Console.WriteLine("Updating Stock for Sale Id: " + sale.Id);

//		List<SaleDetailModel>? saleDetails = await SaleData.LoadSaleDetailBySale(sale.Id);

//		foreach (var product in saleDetails)
//			await ProductStockData.InsertProductStock(new()
//			{
//				Id = 0,
//				ProductId = product.ProductId,
//				Quantity = -product.Quantity,
//				NetRate = product.NetRate,
//				TransactionNo = sale.BillNo,
//				Type = StockType.Sale.ToString(),
//				TransactionId = sale.Id,
//				TransactionDate = DateOnly.FromDateTime(sale.SaleDateTime),
//				LocationId = sale.LocationId
//			});

//		if (sale.PartyId is null || sale.PartyId <= 0)
//			continue;

//		var supplier = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, sale.PartyId.Value);
//		if (supplier.LocationId.HasValue && supplier.LocationId.Value > 0)
//			foreach (var product in saleDetails)
//				await ProductStockData.InsertProductStock(new()
//				{
//					Id = 0,
//					ProductId = product.ProductId,
//					Quantity = product.Quantity,
//					NetRate = product.NetRate,
//					Type = StockType.Purchase.ToString(),
//					TransactionId = sale.Id,
//					TransactionNo = sale.BillNo,
//					TransactionDate = DateOnly.FromDateTime(sale.SaleDateTime),
//					LocationId = supplier.LocationId.Value
//				});
//	}

//	var saleReturns = await CommonData.LoadTableDataByStatus<SaleReturnModel>(TableNames.SaleReturn);
//	foreach (var saleReturn in saleReturns)
//	{
//		Console.WriteLine("Updating RM Stock Kitchen Issue for Kitchen Issue Id: " + saleReturn.Id);

//		var saleDetails = await SaleReturnData.LoadSaleReturnDetailBySaleReturn(saleReturn.Id);

//		foreach (var product in saleDetails)
//			await ProductStockData.InsertProductStock(new()
//			{
//				Id = 0,
//				ProductId = product.ProductId,
//				Quantity = product.Quantity,
//				NetRate = product.NetRate,
//				TransactionNo = saleReturn.BillNo,
//				Type = StockType.SaleReturn.ToString(),
//				TransactionId = saleReturn.Id,
//				TransactionDate = DateOnly.FromDateTime(saleReturn.SaleReturnDateTime),
//				LocationId = saleReturn.LocationId
//			});

//		if (saleReturn.PartyId is null || saleReturn.PartyId <= 0)
//			continue;

//		var supplier = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, saleReturn.PartyId.Value);
//		if (supplier.LocationId.HasValue && supplier.LocationId.Value > 0)
//			foreach (var product in saleDetails)
//				await ProductStockData.InsertProductStock(new()
//				{
//					Id = 0,
//					ProductId = product.ProductId,
//					Quantity = -product.Quantity,
//					NetRate = product.NetRate,
//					Type = StockType.SaleReturn.ToString(),
//					TransactionId = saleReturn.Id,
//					TransactionNo = saleReturn.BillNo,
//					TransactionDate = DateOnly.FromDateTime(saleReturn.SaleReturnDateTime),
//					LocationId = supplier.LocationId.Value
//				});
//	}
//}

//static async Task InsertIssues(ExcelWorksheet worksheet1, ExcelWorksheet worksheet2)
//{
//	Dapper.SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

//	var row = 1;

//	List<KitchenIssueOldModel> kitchenIssue = [];
//	List<KitchenIssueDetailOldModel> kitchenIssueDetails = [];

//	while (worksheet1.Cells[row, 1].Value != null)
//	{
//		var id = worksheet1.Cells[row, 1].Value.ToString();
//		var kitchenId = worksheet1.Cells[row, 2].Value.ToString();
//		var locationId = worksheet1.Cells[row, 3].Value.ToString();
//		var userId = worksheet1.Cells[row, 4].Value.ToString();
//		var billNo = worksheet1.Cells[row, 5].Value.ToString();
//		var billDateTime = worksheet1.Cells[row, 6].Value.ToString();
//		var remarks = worksheet1.Cells[row, 7].Value?.ToString() ?? string.Empty;
//		var status = worksheet1.Cells[row, 9].Value.ToString();

//		kitchenIssue.Add(new()
//		{
//			Id = int.Parse(id),
//			IssueDate = DateTime.Parse(billDateTime),
//			TransactionNo = billNo,
//			KitchenId = int.Parse(kitchenId),
//			LocationId = int.Parse(locationId),
//			Remarks = remarks == "NULL" ? string.Empty : remarks,
//			UserId = int.Parse(userId),
//			CreatedAt = DateTime.Now,
//			Status = status.ToLower() == "true",
//		});

//		row++;
//	}

//	kitchenIssue.RemoveAll(_ => _.Status == false);

//	row = 1;
//	while (worksheet2.Cells[row, 1].Value != null)
//	{
//		var id = worksheet2.Cells[row, 1].Value.ToString();
//		var kitchenIssueId = worksheet2.Cells[row, 2].Value.ToString();
//		var rawMaterialId = worksheet2.Cells[row, 3].Value.ToString();
//		var measurementUnit = worksheet2.Cells[row, 4].Value.ToString();
//		var quantity = worksheet2.Cells[row, 5].Value.ToString();
//		var rate = worksheet2.Cells[row, 6].Value.ToString();
//		var total = worksheet2.Cells[row, 7].Value.ToString();
//		var status = worksheet2.Cells[row, 8].Value.ToString();

//		kitchenIssueDetails.Add(new()
//		{
//			Id = int.Parse(id),
//			KitchenIssueId = int.Parse(kitchenIssueId),
//			RawMaterialId = int.Parse(rawMaterialId),
//			Quantity = decimal.Parse(quantity),
//			MeasurementUnit = measurementUnit,
//			Rate = decimal.Parse(rate),
//			Total = decimal.Parse(total),
//			Status = status.ToLower() == "true",
//		});

//		row++;
//	}

//	kitchenIssueDetails.RemoveAll(_ => _.Status == false);

//	Console.WriteLine("Loaded " + kitchenIssue.Count + " kitchen issues.");
//	Console.WriteLine("Loaded " + kitchenIssueDetails.Count + " kitchen issue details.");

//	foreach (var issue in kitchenIssue)
//	{
//		Console.WriteLine("Inserting Kitchen Issue Id: " + issue.Id);

//		var kitchenIssueDetail = kitchenIssueDetails.Where(pd => pd.KitchenIssueId == issue.Id).ToList();
//		if (kitchenIssueDetail.Count == 0)
//		{
//			Console.WriteLine("No Kitchen Issue Details Found for Kitchen Issue Id: " + issue.Id);
//			continue;
//		}
//		List<KitchenIssueItemCartModel> issueCart = [];

//		foreach (var detail in kitchenIssueDetail)
//			issueCart.Add(new()
//			{
//				ItemId = detail.RawMaterialId,
//				ItemName = "",
//				UnitOfMeasurement = detail.MeasurementUnit,
//				Remarks = null,
//				Quantity = detail.Quantity,
//				Rate = detail.Rate,
//			});

//		foreach (var item in issueCart)
//			item.Total = item.Rate * item.Quantity;

//		KitchenIssueModel finalIssue = new()
//		{
//			Id = 0,
//			TransactionNo = issue.TransactionNo,
//			KitchenId = issue.KitchenId,
//			TransactionDateTime = issue.IssueDate,
//			Remarks = string.IsNullOrWhiteSpace(issue.Remarks) ? null : issue.Remarks,
//			CreatedBy = issue.UserId,
//			TotalAmount = issueCart.Sum(x => x.Total),
//			CompanyId = 1,
//			FinancialYearId = 1,
//			CreatedFromPlatform = "ImportScript",
//			CreatedAt = issue.CreatedAt,
//			Status = true,
//		};

//		await KitchenIssueData.SaveKitchenIssueTransaction(finalIssue, issueCart);
//		Console.WriteLine("Inserted Kitchen Issue Id: " + issue.Id);
//	}

//static async Task UpdateAccounts()
//{
//	Dapper.SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

//	var sale = await CommonData.LoadTableDataByStatus<SaleModel>(TableNames.Sale);
//	foreach (var s in sale)
//	{
//		Console.WriteLine("Updating Accounts for Sale Id: " + s.Id);
//		await SaleData.SaveAccounting(s, false);
//	}

//	var saleReturn = await CommonData.LoadTableDataByStatus<SaleReturnModel>(TableNames.SaleReturn);
//	foreach (var sr in saleReturn)
//	{
//		Console.WriteLine("Updating Accounts for Sale Return Id: " + sr.Id);
//		await SaleReturnData.SaveAccounting(sr, false);
//	}

//	var purchase = await CommonData.LoadTableDataByStatus<PurchaseModel>(TableNames.Purchase);
//	foreach (var p in purchase)
//	{
//		Console.WriteLine("Updating Accounts for Purchase Id: " + p.Id);
//		await PurchaseData.SaveAccounting(p, false);
//	}

//	var purchaseReturn = await CommonData.LoadTableDataByStatus<PurchaseReturnModel>(TableNames.PurchaseReturn);
//	foreach (var pr in purchaseReturn)
//	{
//		Console.WriteLine("Updating Accounts for Purchase Return Id: " + pr.Id);
//		await PurchaseReturnData.SaveAccounting(pr, false);
//	}
//}
//}

//static async Task InsertReturns(ExcelWorksheet worksheet1, ExcelWorksheet worksheet2)
//{
//	Dapper.SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

//	var row = 2;

//	List<SaleReturnOldModel> saleReturn = [];
//	List<SaleReturnDetailOldModel> saleReturnDetails = [];

//	while (worksheet1.Cells[row, 1].Value != null)
//	{
//		var id = worksheet1.Cells[row, 1].Value.ToString();
//		var billNo = worksheet1.Cells[row, 2].Value.ToString();
//		var discPercent = worksheet1.Cells[row, 3].Value.ToString();
//		var rounfOff = worksheet1.Cells[row, 5].Value.ToString();
//		var userId = worksheet1.Cells[row, 7].Value.ToString();
//		var billDateTime = worksheet1.Cells[row, 9].Value.ToString();
//		var partyId = worksheet1.Cells[row, 10].Value.ToString();
//		var cash = worksheet1.Cells[row, 11].Value.ToString();
//		var card = worksheet1.Cells[row, 12].Value.ToString();
//		var upi = worksheet1.Cells[row, 13].Value.ToString();
//		var credit = worksheet1.Cells[row, 14].Value.ToString();
//		var status = worksheet1.Cells[row, 17].Value.ToString();

//		saleReturn.Add(new()
//		{
//			Id = int.Parse(id),
//			BillNo = billNo,
//			PartyId = partyId == "NULL" ? null : int.Parse(partyId),
//			SaleReturnDateTime = DateTime.Parse(billDateTime),
//			Card = decimal.Parse(card),
//			Cash = decimal.Parse(cash),
//			Credit = decimal.Parse(credit),
//			CustomerId = null,
//			DiscPercent = decimal.Parse(discPercent),
//			DiscReason = null,
//			LocationId = 1,
//			RoundOff = decimal.Parse(rounfOff),
//			UPI = decimal.Parse(upi),
//			Remarks = null,
//			UserId = int.Parse(userId),
//			CreatedAt = DateTime.Now,
//			Status = status.ToLower() == "1",
//		});

//		row++;
//	}

//	saleReturn.RemoveAll(_ => _.Status == false);

//	row = 2;
//	while (worksheet2.Cells[row, 1].Value != null)
//	{
//		var id = worksheet2.Cells[row, 1].Value.ToString();
//		var saleReturnId = worksheet2.Cells[row, 2].Value.ToString();
//		var productId = worksheet2.Cells[row, 3].Value.ToString();
//		var quantity = worksheet2.Cells[row, 4].Value.ToString();
//		var rate = worksheet2.Cells[row, 5].Value.ToString();
//		var baseTotal = worksheet2.Cells[row, 6].Value.ToString();
//		var discPercent = worksheet2.Cells[row, 7].Value.ToString();
//		var discAmount = worksheet2.Cells[row, 8].Value.ToString();
//		var afterDiscount = worksheet2.Cells[row, 9].Value.ToString();
//		var cgstPercent = worksheet2.Cells[row, 10].Value.ToString();
//		var cgstAmount = worksheet2.Cells[row, 11].Value.ToString();
//		var sgstPercent = worksheet2.Cells[row, 12].Value.ToString();
//		var sgstAmount = worksheet2.Cells[row, 13].Value.ToString();
//		var igstPercent = worksheet2.Cells[row, 14].Value.ToString();
//		var igstAmount = worksheet2.Cells[row, 15].Value.ToString();
//		var total = worksheet2.Cells[row, 16].Value.ToString();
//		var netRate = worksheet2.Cells[row, 17].Value.ToString();
//		var status = worksheet2.Cells[row, 18].Value.ToString();

//		saleReturnDetails.Add(new()
//		{
//			Id = int.Parse(id),
//			SaleReturnId = int.Parse(saleReturnId),
//			ProductId = int.Parse(productId),
//			Quantity = decimal.Parse(quantity),
//			Rate = decimal.Parse(rate),
//			BaseTotal = decimal.Parse(baseTotal),
//			DiscPercent = decimal.Parse(discPercent),
//			DiscAmount = decimal.Parse(discAmount),
//			AfterDiscount = decimal.Parse(afterDiscount),
//			CGSTPercent = decimal.Parse(cgstPercent),
//			CGSTAmount = decimal.Parse(cgstAmount),
//			SGSTPercent = decimal.Parse(sgstPercent),
//			SGSTAmount = decimal.Parse(sgstAmount),
//			IGSTPercent = decimal.Parse(igstPercent),
//			IGSTAmount = decimal.Parse(igstAmount),
//			NetRate = decimal.Parse(netRate),
//			Total = decimal.Parse(total),
//			Status = status.ToLower() == "1",
//		});

//		row++;
//	}

//	saleReturnDetails.RemoveAll(_ => _.Status == false);

//	Console.WriteLine("Loaded " + saleReturn.Count + " kitchen issues.");
//	Console.WriteLine("Loaded " + saleReturnDetails.Count + " kitchen issue details.");

//	foreach (var issue in saleReturn)
//	{
//		Console.WriteLine("Inserting Kitchen Issue Id: " + issue.Id);

//		var kitchenIssueDetail = saleReturnDetails.Where(pd => pd.SaleReturnId == issue.Id).ToList();
//		if (kitchenIssueDetail.Count == 0)
//		{
//			Console.WriteLine("No Kitchen Issue Details Found for Kitchen Issue Id: " + issue.Id);
//			continue;
//		}
//		List<SaleReturnItemCartModel> returnCart = [];

//		foreach (var detail in kitchenIssueDetail)
//			returnCart.Add(new()
//			{
//				ItemId = detail.ProductId,
//				ItemName = "",
//				Remarks = null,
//				Quantity = detail.Quantity,
//				Rate = detail.Rate,
//				BaseTotal = detail.BaseTotal,
//				DiscountPercent = detail.DiscPercent,
//				DiscountAmount = detail.DiscAmount,
//				CGSTPercent = detail.CGSTPercent,
//				CGSTAmount = detail.CGSTAmount,
//				SGSTPercent = detail.SGSTPercent,
//				SGSTAmount = detail.SGSTAmount,
//				IGSTPercent = detail.IGSTPercent,
//				IGSTAmount = detail.IGSTAmount,
//				AfterDiscount = detail.AfterDiscount,
//				TotalTaxAmount = detail.CGSTAmount + detail.SGSTAmount + detail.IGSTAmount,
//				InclusiveTax = false,
//				Total = detail.Total,
//				NetRate = detail.NetRate,
//			});


//		SaleReturnModel finalIssue = new()
//		{
//			Id = 0,
//			TransactionNo = issue.BillNo,
//			CustomerId = null,
//			ItemsTotalAmount = returnCart.Sum(x => x.Total),
//			OtherChargesPercent = 0,
//			OtherChargesAmount = 0,
//			LocationId = 1,
//			PartyId = issue.PartyId,
//			RoundOffAmount = issue.RoundOff,
//			DiscountPercent = issue.DiscPercent,
//			DiscountAmount = returnCart.Sum(x => x.Total) * (issue.DiscPercent / 100),
//			TransactionDateTime = issue.SaleReturnDateTime,
//			Remarks = string.IsNullOrWhiteSpace(issue.Remarks) ? null : issue.Remarks,
//			CreatedBy = issue.UserId,
//			TotalAmount = issue.Cash + issue.Card + issue.Credit + issue.UPI,
//			Card = issue.Card,
//			Cash = issue.Cash,
//			Credit = issue.Credit,
//			UPI = issue.UPI,
//			CompanyId = 1,
//			FinancialYearId = 1,
//			CreatedFromPlatform = "ImportScript",
//			CreatedAt = issue.CreatedAt,
//			Status = true,
//		};

//		if (finalIssue.Credit > 0 && finalIssue.PartyId is null)
//		{
//			finalIssue.Cash += finalIssue.Credit;
//			finalIssue.Credit = 0;
//		}
//		await SaleReturnData.SaveSaleReturnTransaction(finalIssue, returnCart);
//		Console.WriteLine("Inserted Kitchen Issue Id: " + issue.Id);
//	}
//}

//static async Task InsertSales(ExcelWorksheet worksheet1, ExcelWorksheet worksheet2)
//{
//	Dapper.SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
//	var row = 2;

//	List<SaleOldModel> sale = [];
//	List<SaleDetailOldModel> saleDetails = [];

//	while (worksheet1.Cells[row, 1].Value != null)
//	{
//		var id = worksheet1.Cells[row, 1].Value.ToString();
//		var billNo = worksheet1.Cells[row, 2].Value.ToString();
//		var discPercent = worksheet1.Cells[row, 3].Value.ToString();
//		var rounfOff = worksheet1.Cells[row, 5].Value.ToString();
//		var userId = worksheet1.Cells[row, 7].Value.ToString();
//		var locationId = worksheet1.Cells[row, 8].Value.ToString();
//		var billDateTime = worksheet1.Cells[row, 9].Value.ToString();
//		var partyId = worksheet1.Cells[row, 10].Value.ToString();
//		var orderId = worksheet1.Cells[row, 11].Value.ToString();
//		var cash = worksheet1.Cells[row, 12].Value.ToString();
//		var card = worksheet1.Cells[row, 13].Value.ToString();
//		var upi = worksheet1.Cells[row, 14].Value.ToString();
//		var credit = worksheet1.Cells[row, 15].Value.ToString();
//		var customerId = worksheet1.Cells[row, 16].Value.ToString();
//		var status = worksheet1.Cells[row, 18].Value.ToString();

//		sale.Add(new()
//		{
//			Id = int.Parse(id),
//			BillNo = billNo,
//			PartyId = partyId == "NULL" ? null : int.Parse(partyId),
//			OrderId = orderId == "NULL" ? null : int.Parse(orderId),
//			SaleDateTime = DateTime.Parse(billDateTime),
//			Card = decimal.Parse(card),
//			Cash = decimal.Parse(cash),
//			Credit = decimal.Parse(credit),
//			CustomerId = customerId == "NULL" ? null : int.Parse(customerId),
//			DiscPercent = decimal.Parse(discPercent),
//			DiscReason = null,
//			LocationId = int.Parse(locationId),
//			RoundOff = decimal.Parse(rounfOff),
//			UPI = decimal.Parse(upi),
//			Remarks = null,
//			UserId = int.Parse(userId),
//			CreatedAt = DateTime.Now,
//			Status = status.ToLower() == "1",
//		});

//		row++;
//	}

//	sale.RemoveAll(_ => _.Status == false);

//	row = 2;
//	while (worksheet2.Cells[row, 1].Value != null)
//	{
//		var id = worksheet2.Cells[row, 1].Value.ToString();
//		var saleId = worksheet2.Cells[row, 2].Value.ToString();
//		var productId = worksheet2.Cells[row, 3].Value.ToString();
//		var quantity = worksheet2.Cells[row, 4].Value.ToString();
//		var rate = worksheet2.Cells[row, 5].Value.ToString();
//		var baseTotal = worksheet2.Cells[row, 6].Value.ToString();
//		var discPercent = worksheet2.Cells[row, 7].Value.ToString();
//		var discAmount = worksheet2.Cells[row, 8].Value.ToString();
//		var afterDiscount = worksheet2.Cells[row, 9].Value.ToString();
//		var cgstPercent = worksheet2.Cells[row, 10].Value.ToString();
//		var cgstAmount = worksheet2.Cells[row, 11].Value.ToString();
//		var sgstPercent = worksheet2.Cells[row, 12].Value.ToString();
//		var sgstAmount = worksheet2.Cells[row, 13].Value.ToString();
//		var igstPercent = worksheet2.Cells[row, 14].Value.ToString();
//		var igstAmount = worksheet2.Cells[row, 15].Value.ToString();
//		var total = worksheet2.Cells[row, 16].Value.ToString();
//		var netRate = worksheet2.Cells[row, 17].Value.ToString();
//		var status = worksheet2.Cells[row, 18].Value.ToString();

//		saleDetails.Add(new()
//		{
//			Id = int.Parse(id),
//			SaleId = int.Parse(saleId),
//			ProductId = int.Parse(productId),
//			Quantity = decimal.Parse(quantity),
//			Rate = decimal.Parse(rate),
//			BaseTotal = decimal.Parse(baseTotal),
//			DiscPercent = decimal.Parse(discPercent),
//			DiscAmount = decimal.Parse(discAmount),
//			AfterDiscount = decimal.Parse(afterDiscount),
//			CGSTPercent = decimal.Parse(cgstPercent),
//			CGSTAmount = decimal.Parse(cgstAmount),
//			SGSTPercent = decimal.Parse(sgstPercent),
//			SGSTAmount = decimal.Parse(sgstAmount),
//			IGSTPercent = decimal.Parse(igstPercent),
//			IGSTAmount = decimal.Parse(igstAmount),
//			NetRate = decimal.Parse(netRate),
//			Total = decimal.Parse(total),
//			Status = status.ToLower() == "1",
//		});

//		row++;
//	}

//	saleDetails.RemoveAll(_ => _.Status == false);

//	Console.WriteLine("Loaded " + sale.Count + " kitchen issues.");
//	Console.WriteLine("Loaded " + saleDetails.Count + " kitchen issue details.");

//	foreach (var issue in sale)
//	{
//		Console.WriteLine("Inserting Kitchen Issue Id: " + issue.Id);

//		var kitchenIssueDetail = saleDetails.Where(pd => pd.SaleId == issue.Id).ToList();
//		if (kitchenIssueDetail.Count == 0)
//		{
//			Console.WriteLine("No Kitchen Issue Details Found for Kitchen Issue Id: " + issue.Id);
//			continue;
//		}
//		List<SaleItemCartModel> returnCart = [];

//		foreach (var detail in kitchenIssueDetail)
//			returnCart.Add(new()
//			{
//				ItemId = detail.ProductId,
//				ItemName = "",
//				Remarks = null,
//				Quantity = detail.Quantity,
//				Rate = detail.Rate,
//				BaseTotal = detail.BaseTotal,
//				DiscountPercent = detail.DiscPercent,
//				DiscountAmount = detail.DiscAmount,
//				CGSTPercent = detail.CGSTPercent,
//				CGSTAmount = detail.CGSTAmount,
//				SGSTPercent = detail.SGSTPercent,
//				SGSTAmount = detail.SGSTAmount,
//				IGSTPercent = detail.IGSTPercent,
//				IGSTAmount = detail.IGSTAmount,
//				AfterDiscount = detail.AfterDiscount,
//				TotalTaxAmount = detail.CGSTAmount + detail.SGSTAmount + detail.IGSTAmount,
//				InclusiveTax = false,
//				Total = detail.Total,
//				NetRate = detail.NetRate,
//			});


//		SaleModel finalIssue = new()
//		{
//			Id = 0,
//			TransactionNo = issue.BillNo,
//			CustomerId = issue.CustomerId,
//			ItemsTotalAmount = returnCart.Sum(x => x.Total),
//			OtherChargesPercent = 0,
//			OtherChargesAmount = 0,
//			OrderId = issue.OrderId,
//			LocationId = issue.LocationId,
//			PartyId = issue.PartyId,
//			RoundOffAmount = issue.RoundOff,
//			DiscountPercent = issue.DiscPercent,
//			DiscountAmount = returnCart.Sum(x => x.Total) * (issue.DiscPercent / 100),
//			TransactionDateTime = issue.SaleDateTime,
//			Remarks = string.IsNullOrWhiteSpace(issue.Remarks) ? null : issue.Remarks,
//			CreatedBy = issue.UserId,
//			TotalAmount = issue.Cash + issue.Card + issue.Credit + issue.UPI,
//			Card = issue.Card,
//			Cash = issue.Cash,
//			Credit = issue.Credit,
//			UPI = issue.UPI,
//			CompanyId = 1,
//			FinancialYearId = 1,
//			CreatedFromPlatform = "ImportScript",
//			CreatedAt = issue.CreatedAt,
//			Status = true,
//		};

//		finalIssue.TransactionNo = await GenerateCodes.GenerateSaleTransactionNo(finalIssue);

//		if (finalIssue.Credit > 0 && finalIssue.PartyId is null)
//		{
//			finalIssue.Cash += finalIssue.Credit;
//			finalIssue.Credit = 0;
//		}
//		await SaleData.SaveSaleTransaction(finalIssue, returnCart);
//		Console.WriteLine("Inserted Kitchen Issue Id: " + issue.Id);
//	}
//}

//static async Task InsertOrder(ExcelWorksheet worksheet1, ExcelWorksheet worksheet2)
//{
//	Dapper.SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
//	var row = 2;

//	List<OrderModelOld> orders = [];
//	List<OrderDetailModelOld> orderDetails = [];

//	while (worksheet1.Cells[row, 1].Value != null)
//	{
//		var id = worksheet1.Cells[row, 1].Value.ToString();
//		var billNo = worksheet1.Cells[row, 2].Value.ToString();
//		var billDateTime = worksheet1.Cells[row, 3].Value.ToString();
//		var locationId = worksheet1.Cells[row, 4].Value.ToString();
//		var userId = worksheet1.Cells[row, 5].Value.ToString();
//		var saleId = worksheet1.Cells[row, 7].Value.ToString();
//		var status = worksheet1.Cells[row, 9].Value.ToString();


//		orders.Add(new()
//		{
//			Id = int.Parse(id),
//			OrderNo = billNo,
//			OrderDateTime = DateTime.Parse(billDateTime),
//			LocationId = int.Parse(locationId),
//			UserId = int.Parse(userId),
//			Remarks = "",
//			SaleId = saleId == "NULL" ? null : int.Parse(saleId),
//			CreatedAt = DateTime.Now,
//			Status = status.ToLower() == "1",
//		});

//		row++;
//	}

//	orders.RemoveAll(_ => _.Status == false);

//	row = 2;
//	while (worksheet2.Cells[row, 1].Value != null)
//	{
//		var id = worksheet2.Cells[row, 1].Value.ToString();
//		var orderId = worksheet2.Cells[row, 2].Value.ToString();
//		var productId = worksheet2.Cells[row, 3].Value.ToString();
//		var quantity = worksheet2.Cells[row, 4].Value.ToString();
//		var status = worksheet2.Cells[row, 5].Value.ToString();

//		orderDetails.Add(new()
//		{
//			Id = int.Parse(id),
//			OrderId = int.Parse(orderId),
//			ProductId = int.Parse(productId),
//			Quantity = decimal.Parse(quantity),
//			Status = status.ToLower() == "1",
//		});

//		row++;
//	}

//	orderDetails.RemoveAll(_ => _.Status == false);

//	Console.WriteLine("Loaded " + orders.Count + " kitchen issues.");
//	Console.WriteLine("Loaded " + orderDetails.Count + " kitchen issue details.");

//	foreach (var issue in orders)
//	{
//		Console.WriteLine("Inserting Kitchen Issue Id: " + issue.Id);

//		var kitchenIssueDetail = orderDetails.Where(pd => pd.OrderId == issue.Id).ToList();
//		if (kitchenIssueDetail.Count == 0)
//		{
//			Console.WriteLine("No Kitchen Issue Details Found for Kitchen Issue Id: " + issue.Id);
//			continue;
//		}
//		List<OrderItemCartModel> returnCart = [];

//		foreach (var detail in kitchenIssueDetail)
//			returnCart.Add(new()
//			{
//				ItemId = detail.ProductId,
//				ItemName = "",
//				Remarks = null,
//				Quantity = detail.Quantity,
//			});


//		OrderModel finalIssue = new()
//		{
//			Id = 0,
//			SaleId = issue.SaleId,
//			TransactionNo = issue.OrderNo,
//			LocationId = issue.LocationId,
//			TransactionDateTime = issue.OrderDateTime,
//			Remarks = string.IsNullOrWhiteSpace(issue.Remarks) ? null : issue.Remarks,
//			CreatedBy = issue.UserId,
//			CompanyId = 1,
//			FinancialYearId = 1,
//			CreatedFromPlatform = "ImportScript",
//			CreatedAt = issue.CreatedAt,
//			Status = true,
//		};

//		finalIssue.TransactionNo = await GenerateCodes.GenerateOrderTransactionNo(finalIssue);

//		finalIssue.Id = await OrderData.SaveOrderTransaction(finalIssue, returnCart);

//		if (finalIssue.SaleId is not null && finalIssue.SaleId > 0)
//		{
//			var sale = await CommonData.LoadTableDataById<SaleModel>(TableNames.Sale, finalIssue.SaleId.Value);
//			if (sale is not null)
//			{
//				sale.OrderId = finalIssue.Id;
//				await SaleData.InsertSale(sale);
//			}
//		}

//		Console.WriteLine("Inserted Kitchen Issue Id: " + issue.Id);
//	}
//}

//static async Task FixMasterNames()
//{
//	var products = await CommonData.LoadTableData<LedgerModel>(TableNames.Ledger);

//	products = products.Where(p => p.Name.EndsWith("&#X20;")).ToList();

//	foreach (var product in products)
//	{
//		product.Name = product.Name.Replace("&#X20;", " ").TrimEnd();
//		await LedgerData.InsertLedger(product);
//	}
//}

//static async Task FixLedgers()
//{
//	var ledgers = await CommonData.LoadTableData<LedgerModel>(TableNames.Ledger);

//	foreach (var ledger in ledgers)
//	{
//		if (string.IsNullOrWhiteSpace(ledger.GSTNo?.Trim()))
//			ledger.GSTNo = null;

//		if (string.IsNullOrWhiteSpace(ledger.PANNo?.Trim()))
//			ledger.PANNo = null;

//		if (string.IsNullOrWhiteSpace(ledger.Phone?.Trim()))
//			ledger.Phone = null;

//		if (string.IsNullOrWhiteSpace(ledger.Email?.Trim()))
//			ledger.Email = null;

//		if (string.IsNullOrWhiteSpace(ledger.Address?.Trim()))
//			ledger.Address = null;

//		if (string.IsNullOrWhiteSpace(ledger.Remarks?.Trim()))
//			ledger.Remarks = null;

//		if (string.IsNullOrWhiteSpace(ledger.Alias?.Trim()))
//			ledger.Alias = null;

//		if (string.IsNullOrWhiteSpace(ledger.CINNo?.Trim()))
//			ledger.CINNo = null;

//		await LedgerData.InsertLedger(ledger);

//		Console.WriteLine("Fixed Ledger Id: " + ledger.Id);
//	}
//}

//class PurchaseModelOld
//{
//	public int Id { get; set; }
//	public string BillNo { get; set; }
//	public int SupplierId { get; set; }
//	public DateTime BillDateTime { get; set; }
//	public decimal CDPercent { get; set; }
//	public decimal RoundOff { get; set; }
//	public string Remarks { get; set; }
//	public int UserId { get; set; }
//	public DateTime CreatedAt { get; set; }
//	public bool Status { get; set; }
//}

//class PurchaseDetailModelOld
//{
//	public int Id { get; set; }
//	public int PurchaseId { get; set; }
//	public int RawMaterialId { get; set; }
//	public decimal Quantity { get; set; }
//	public string MeasurementUnit { get; set; }
//	public decimal Rate { get; set; }
//	public decimal BaseTotal { get; set; }
//	public decimal DiscPercent { get; set; }
//	public decimal DiscAmount { get; set; }
//	public decimal AfterDiscount { get; set; }
//	public decimal CGSTPercent { get; set; }
//	public decimal CGSTAmount { get; set; }
//	public decimal SGSTPercent { get; set; }
//	public decimal SGSTAmount { get; set; }
//	public decimal IGSTPercent { get; set; }
//	public decimal IGSTAmount { get; set; }
//	public decimal Total { get; set; }
//	public decimal NetRate { get; set; }
//	public bool Status { get; set; }
//}

//class KitchenIssueOldModel
//{
//	public int Id { get; set; }
//	public int KitchenId { get; set; }
//	public int LocationId { get; set; }
//	public int UserId { get; set; }
//	public string TransactionNo { get; set; }
//	public DateTime IssueDate { get; set; }
//	public string Remarks { get; set; }
//	public DateTime CreatedAt { get; set; }
//	public bool Status { get; set; }
//}

//class KitchenIssueDetailOldModel
//{
//	public int Id { get; set; }
//	public int KitchenIssueId { get; set; }
//	public int RawMaterialId { get; set; }
//	public string MeasurementUnit { get; set; }
//	public decimal Quantity { get; set; }
//	public decimal Rate { get; set; }
//	public decimal Total { get; set; }
//	public bool Status { get; set; }
//}

//class SaleReturnOldModel
//{
//	public int Id { get; set; }
//	public string BillNo { get; set; }
//	public decimal DiscPercent { get; set; }
//	public string DiscReason { get; set; }
//	public decimal RoundOff { get; set; }
//	public string Remarks { get; set; }
//	public int UserId { get; set; }
//	public int LocationId { get; set; }
//	public DateTime SaleReturnDateTime { get; set; }
//	public int? PartyId { get; set; }
//	public decimal Cash { get; set; }
//	public decimal Card { get; set; }
//	public decimal UPI { get; set; }
//	public decimal Credit { get; set; }
//	public int? CustomerId { get; set; }
//	public DateTime CreatedAt { get; set; }
//	public bool Status { get; set; }
//}

//class SaleReturnDetailOldModel
//{
//	public int Id { get; set; }
//	public int SaleReturnId { get; set; }
//	public int ProductId { get; set; }
//	public decimal Quantity { get; set; }
//	public decimal Rate { get; set; }
//	public decimal BaseTotal { get; set; }
//	public decimal DiscPercent { get; set; }
//	public decimal DiscAmount { get; set; }
//	public decimal AfterDiscount { get; set; }
//	public decimal CGSTPercent { get; set; }
//	public decimal CGSTAmount { get; set; }
//	public decimal SGSTPercent { get; set; }
//	public decimal SGSTAmount { get; set; }
//	public decimal IGSTPercent { get; set; }
//	public decimal IGSTAmount { get; set; }
//	public decimal Total { get; set; }
//	public decimal NetRate { get; set; }
//	public bool Status { get; set; }
//}

//class SaleOldModel
//{
//	public int Id { get; set; }
//	public string BillNo { get; set; }
//	public decimal DiscPercent { get; set; }
//	public string DiscReason { get; set; }
//	public decimal RoundOff { get; set; }
//	public string Remarks { get; set; }
//	public int UserId { get; set; }
//	public int LocationId { get; set; }
//	public DateTime SaleDateTime { get; set; }
//	public int? PartyId { get; set; }
//	public int? OrderId { get; set; }
//	public decimal Cash { get; set; }
//	public decimal Card { get; set; }
//	public decimal UPI { get; set; }
//	public decimal Credit { get; set; }
//	public int? CustomerId { get; set; }
//	public DateTime CreatedAt { get; set; }
//	public bool Status { get; set; }
//}

//class SaleDetailOldModel
//{
//	public int Id { get; set; }
//	public int SaleId { get; set; }
//	public int ProductId { get; set; }
//	public decimal Quantity { get; set; }
//	public decimal Rate { get; set; }
//	public decimal BaseTotal { get; set; }
//	public decimal DiscPercent { get; set; }
//	public decimal DiscAmount { get; set; }
//	public decimal AfterDiscount { get; set; }
//	public decimal CGSTPercent { get; set; }
//	public decimal CGSTAmount { get; set; }
//	public decimal SGSTPercent { get; set; }
//	public decimal SGSTAmount { get; set; }
//	public decimal IGSTPercent { get; set; }
//	public decimal IGSTAmount { get; set; }
//	public decimal Total { get; set; }
//	public decimal NetRate { get; set; }
//	public bool Status { get; set; }
//}

//public class OrderModelOld
//{
//	public int Id { get; set; }
//	public string OrderNo { get; set; }
//	public DateTime OrderDateTime { get; set; }
//	public int LocationId { get; set; }
//	public int UserId { get; set; }
//	public string Remarks { get; set; }
//	public int? SaleId { get; set; }
//	public DateTime CreatedAt { get; set; }
//	public bool Status { get; set; }
//}

//public class OrderDetailModelOld
//{
//	public int Id { get; set; }
//	public int OrderId { get; set; }
//	public int ProductId { get; set; }
//	public decimal Quantity { get; set; }
//	public bool Status { get; set; }
//}
#endregion

static async Task RecalculateTransactions()
{
	Dapper.SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

	var purchase = await CommonData.LoadTableData<PurchaseModel>(TableNames.Purchase);
	foreach (var p in purchase)
	{
		Console.WriteLine("Recalculating Transaction: " + p.Id);
		var details = await CommonData.LoadTableDataByMasterId<PurchaseDetailModel>(TableNames.PurchaseDetail, p.Id);
		p.TotalItems = details.Count;
		p.TotalQuantity = details.Sum(d => d.Quantity);
		p.BaseTotal = details.Sum(d => d.BaseTotal);
		p.ItemDiscountAmount = details.Sum(d => d.DiscountAmount);
		p.TotalAfterItemDiscount = details.Sum(d => d.AfterDiscount);
		p.TotalExtraTaxAmount = details.Where(d => d.InclusiveTax == false).Sum(d => d.TotalTaxAmount);
		p.TotalInclusiveTaxAmount = details.Where(d => d.InclusiveTax == true).Sum(d => d.TotalTaxAmount);
		p.TotalAfterTax = details.Sum(d => d.Total);
		await PurchaseData.InsertPurchase(p);
	}
}