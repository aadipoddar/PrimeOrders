using OfficeOpenXml;

using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory;
using PrimeBakesLibrary.Data.Product;
using PrimeBakesLibrary.Data.Sale;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Product;
using PrimeBakesLibrary.Models.Sale;

//FileInfo fileInfo = new(@"C:\Others\accountingdetail.xlsx");

//ExcelPackage.License.SetNonCommercialPersonal("AadiSoft");

//using var package = new ExcelPackage(fileInfo);

//await package.LoadAsync(fileInfo);

//var worksheet = package.Workbook.Worksheets[0];

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

await UpdateProductLocation();

Console.WriteLine("Finished importing Items.");
Console.ReadLine();

static async Task UpdateProducts()
{
	var products = await CommonData.LoadTableData<ProductModel>(TableNames.Product);
	int row = 1;

	foreach (var product in products)
	{
		Console.WriteLine(product.Name);

		var code = "FP" + row.ToString("D4");

		await ProductData.InsertProduct(new()
		{
			Id = product.Id,
			Code = code,
			Name = product.Name,
			ProductCategoryId = product.ProductCategoryId,
			Rate = product.Rate,
			TaxId = product.TaxId,
			Status = product.Status
		});

		Console.WriteLine("Updated Product: " + product.Name + " with code " + code);
		row++;
	}
}

static async Task InsertRawMaterial(ExcelWorksheet worksheet)
{
	int row = 1;

	while (worksheet.Cells[row, 2].Value != null)
	{
		var name = worksheet.Cells[row, 2].Value.ToString();
		var unit = worksheet.Cells[row, 3].Value?.ToString();
		var code = "RM" + row.ToString("D4");

		if (string.IsNullOrWhiteSpace(unit))
			unit = "KG";

		if (string.IsNullOrWhiteSpace(name))
		{
			Console.WriteLine("Not Inserted Row = " + row);
			continue;
		}

		unit = unit.Replace(" ", "");

		Console.WriteLine("Inserting New Raw Material: " + name + " with unit " + unit);
		await RawMaterialData.InsertRawMaterial(new()
		{
			Id = row,
			Code = code,
			Name = name,
			MRP = 0,
			RawMaterialCategoryId = 1,
			MeasurementUnit = unit,
			TaxId = 7,
			Status = true
		});

		row++;
	}
}

static async Task InsertProducts(ExcelWorksheet worksheet)
{
	int row = 1;

	while (worksheet.Cells[row, 1].Value != null)
	{
		var code = "FP" + row.ToString("D4");
		var name = worksheet.Cells[row, 1].Value.ToString();
		var categoryId = worksheet.Cells[row, 2].Value.ToString();
		var taxId = 7;
		var price = 0;

		if (string.IsNullOrWhiteSpace(code) ||
			string.IsNullOrWhiteSpace(name) ||
			string.IsNullOrWhiteSpace(categoryId))
		{
			Console.WriteLine("Not Inserted Row = " + row);
			continue;
		}

		code = code.Replace(" ", "");

		Console.WriteLine("Inserting New Product: " + name + " and code " + code);
		await ProductData.InsertProduct(new()
		{
			Id = 0,
			Code = code,
			Name = name,
			ProductCategoryId = int.Parse(categoryId),
			Rate = price,
			TaxId = taxId,
			Status = true
		});

		row++;
	}
}

static async Task InsertSupplier(ExcelWorksheet worksheet)
{
	int row = 2;

	while (worksheet.Cells[row, 1].Value != null)
	{
		var code = "LD" + (row - 1).ToString("D5");
		var name = worksheet.Cells[row, 1].Value.ToString();
		var address = worksheet.Cells[row, 2].Value?.ToString() ?? string.Empty;
		var remarks = worksheet.Cells[row, 3].Value?.ToString() ?? string.Empty;
		var accountType = worksheet.Cells[row, 4].Value?.ToString() ?? string.Empty;
		var phone = worksheet.Cells[row, 5].Value?.ToString() ?? string.Empty;
		var gstNo = worksheet.Cells[row, 6].Value?.ToString() ?? string.Empty;

		if (string.IsNullOrWhiteSpace(name))
		{
			Console.WriteLine("Not Inserted Row = " + row);
			continue;
		}

		Console.WriteLine("Inserting Ledger: " + name + " and code " + code);
		await LedgerData.InsertLedger(new()
		{
			Id = 0,
			Code = code,
			Name = name,
			Address = address ?? string.Empty,
			GSTNo = gstNo ?? string.Empty,
			Phone = phone ?? string.Empty,
			StateId = 1,
			LocationId = null,
			Remarks = remarks ?? string.Empty,
			AccountTypeId = int.Parse(accountType),
			GroupId = 1,
			Status = true
		});

		row++;
	}
}

static async Task InsertProductLocations()
{
	var products = await CommonData.LoadTableData<ProductModel>(TableNames.Product);
	var locations = await CommonData.LoadTableData<LocationModel>(TableNames.Location);

	foreach (var product in products)
		foreach (var location in locations)
			await ProductData.InsertProductLocation(new()
			{
				Id = 0,
				ProductId = product.Id,
				LocationId = location.Id,
				Rate = product.Rate,
				Status = true
			});
}

static async Task InsertFixProductLocations(ExcelWorksheet worksheet)
{
	int row = 2;
	while (worksheet.Cells[row, 1].Value != null)
	{
		var productId = worksheet.Cells[row, 1].Value.ToString();
		var rate = worksheet.Cells[row, 2].Value.ToString();
		var locationId = worksheet.Cells[row, 3].Value.ToString();

		if (string.IsNullOrWhiteSpace(productId) ||
			string.IsNullOrWhiteSpace(locationId) ||
			string.IsNullOrWhiteSpace(rate))
		{
			Console.WriteLine("Not Inserted Row = " + row);
			continue;
		}

		Console.WriteLine("Inserting Product Location for ProductId: " + productId + " and LocationId " + locationId);

		var product = (await ProductData.LoadProductRateByProduct(int.Parse(productId))).Where(p => p.LocationId == int.Parse(locationId)).FirstOrDefault();
		if (product is null)
		{
			Console.WriteLine("Product Not Found for ProductId: " + productId + " and LocationId " + locationId);
			row++;
			continue;
		}

		await ProductData.InsertProductLocation(new()
		{
			Id = product.Id,
			ProductId = int.Parse(productId),
			LocationId = int.Parse(locationId),
			Rate = decimal.Parse(rate),
			Status = true
		});
		row++;
	}
}

static async Task InsertAccounting(ExcelWorksheet worksheet)
{
	Dapper.SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
	int row = 2;
	while (worksheet.Cells[row, 1].Value != null)
	{
		var referenceNo = worksheet.Cells[row, 2].Value.ToString();
		var voucherId = worksheet.Cells[row, 3].Value.ToString();
		var remarks = worksheet.Cells[row, 4].Value?.ToString() ?? string.Empty;
		var date = worksheet.Cells[row, 5].Value.ToString();
		var financialYearId = worksheet.Cells[row, 6].Value.ToString();
		var userId = worksheet.Cells[row, 7].Value.ToString();
		var generatedMode = worksheet.Cells[row, 8].Value.ToString();
		var createdAt = worksheet.Cells[row, 9].Value.ToString();
		var status = worksheet.Cells[row, 10].Value.ToString();

		Console.WriteLine("Inserting Accounting Voucher: " + referenceNo + " and voucherId " + voucherId);
		await AccountingData.InsertAccounting(new()
		{
			Id = 0,
			TransactionNo = referenceNo,
			UserId = int.Parse(userId),
			VoucherId = int.Parse(voucherId),
			Remarks = remarks,
			FinancialYearId = int.Parse(financialYearId),
			GeneratedModule = generatedMode,
			AccountingDate = DateOnly.FromDateTime(DateTime.Parse(date)),
			CreatedAt = DateTime.Now,
			Status = status.ToLower() == "true",
		});

		row++;
	}
}

static async Task InsertAccountingDetails(ExcelWorksheet worksheet)
{
	Dapper.SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
	int row = 2;
	while (worksheet.Cells[row, 1].Value != null)
	{
		var accountingId = worksheet.Cells[row, 2].Value?.ToString();
		var ledgerId = worksheet.Cells[row, 3].Value?.ToString();
		var debit = worksheet.Cells[row, 4].Value?.ToString();
		var credit = worksheet.Cells[row, 5].Value?.ToString();
		var remarks = worksheet.Cells[row, 6].Value?.ToString() ?? string.Empty;
		var status = worksheet.Cells[row, 7].Value?.ToString();

		// Skip row if essential fields are null or empty
		if (string.IsNullOrWhiteSpace(accountingId) || string.IsNullOrWhiteSpace(ledgerId) || string.IsNullOrWhiteSpace(status))
		{
			Console.WriteLine("Skipping row " + row + " due to missing required fields");
			row++;
			continue;
		}

		var accounting = await CommonData.LoadTableDataById<AccountingModel>(TableNames.Accounting, int.Parse(accountingId));

		int? id = null;
		if (accounting.GeneratedModule == "Sales")
		{
			var sale = await SaleData.LoadSaleByBillNo(accounting.TransactionNo);
			id = sale?.Id;
		}
		else if (accounting.GeneratedModule == "SaleReturn")
		{
			var saleReturn = await SaleReturnData.LoadSaleReturnByBillNo(accounting.TransactionNo);
			id = saleReturn?.Id;
		}
		else if (accounting.GeneratedModule == "Purchase")
			id = (await CommonData.LoadTableData<PurchaseModel>(TableNames.Purchase)).Where(p => p.BillNo == accounting.TransactionNo).FirstOrDefault()?.Id ?? 0;

		Console.WriteLine("Inserting Accounting Voucher: " + accounting.TransactionNo + " and voucherId " + accounting.VoucherId);

		// Helper method to safely parse decimal values
		decimal? ParseNullableDecimal(string? value)
		{
			if (string.IsNullOrWhiteSpace(value) || value.Equals("NULL", StringComparison.OrdinalIgnoreCase))
				return null;
			return decimal.TryParse(value, out var result) ? result : null;
		}

		await AccountingData.InsertAccountingDetails(new()
		{
			Id = 0,
			AccountingId = int.Parse(accountingId),
			LedgerId = int.Parse(ledgerId),
			ReferenceId = id,
			ReferenceType = id is not null ? accounting.GeneratedModule : null,
			Credit = ParseNullableDecimal(credit),
			Debit = ParseNullableDecimal(debit),
			Remarks = remarks,
			Status = status.Equals("true", StringComparison.OrdinalIgnoreCase),
		});

		row++;
	}
}

static async Task RecalculateBills()
{
	var allSales = await CommonData.LoadTableDataByStatus<SaleModel>(TableNames.Sale);
	foreach (var sale in allSales)
	{
		var saleDetails = await SaleData.LoadSaleDetailBySale(sale.Id);

		Console.WriteLine("Recalculating Bill No: " + sale.BillNo);

		foreach (var item in saleDetails)
		{
			var baseTotal = item.Quantity * item.Rate;
			var cgstAmount = baseTotal * item.CGSTPercent / 100;
			var sgstAmount = baseTotal * item.SGSTPercent / 100;
			var igstAmount = baseTotal * item.IGSTPercent / 100;
			var total = baseTotal + cgstAmount + sgstAmount + igstAmount;
			// Add 0 Check
			decimal netRate = 0;
			if (item.Quantity > 0)
				netRate = total / item.Quantity * (1 - (sale.DiscPercent / 100));

			await SaleData.InsertSaleDetail(new()
			{
				Id = item.Id,
				SaleId = item.SaleId,
				ProductId = item.ProductId,
				Quantity = item.Quantity,
				Rate = item.Rate,
				BaseTotal = baseTotal,
				DiscPercent = 0,
				DiscAmount = 0,
				AfterDiscount = baseTotal,
				CGSTPercent = item.CGSTPercent,
				CGSTAmount = cgstAmount,
				SGSTPercent = item.SGSTPercent,
				SGSTAmount = sgstAmount,
				IGSTPercent = item.IGSTPercent,
				IGSTAmount = igstAmount,
				Total = total,
				NetRate = netRate,
				Status = true
			});
		}

		var newSaleDetails = await SaleData.LoadSaleDetailBySale(sale.Id);
		var newTotal = newSaleDetails.Sum(d => d.Total) * (1 - (sale.DiscPercent / 100));
		sale.RoundOff = Math.Round(newTotal) - newTotal;

		if (sale.Cash > 0)
			sale.Cash = Math.Round(newTotal);
		if (sale.Card > 0)
			sale.Card = Math.Round(newTotal);
		if (sale.UPI > 0)
			sale.UPI = Math.Round(newTotal);
		if (sale.Credit > 0)
			sale.Credit = Math.Round(newTotal);

		await SaleData.InsertSale(sale);
	}
}

static async Task RecalculateReturnBills()
{
	var allSales = await CommonData.LoadTableDataByStatus<SaleReturnModel>(TableNames.SaleReturn);
	foreach (var sale in allSales)
	{
		var saleDetails = await SaleReturnData.LoadSaleReturnDetailBySaleReturn(sale.Id);

		Console.WriteLine("Recalculating Bill No: " + sale.BillNo);

		foreach (var item in saleDetails)
		{
			var baseTotal = item.Quantity * item.Rate;
			var cgstAmount = baseTotal * item.CGSTPercent / 100;
			var sgstAmount = baseTotal * item.SGSTPercent / 100;
			var igstAmount = baseTotal * item.IGSTPercent / 100;
			var total = baseTotal + cgstAmount + sgstAmount + igstAmount;
			// Add 0 Check
			decimal netRate = 0;
			if (item.Quantity > 0)
				netRate = total / item.Quantity * (1 - (sale.DiscPercent / 100));

			await SaleReturnData.InsertSaleReturnDetail(new()
			{
				Id = item.Id,
				SaleReturnId = item.SaleReturnId,
				ProductId = item.ProductId,
				Quantity = item.Quantity,
				Rate = item.Rate,
				BaseTotal = baseTotal,
				DiscPercent = 0,
				DiscAmount = 0,
				AfterDiscount = baseTotal,
				CGSTPercent = item.CGSTPercent,
				CGSTAmount = cgstAmount,
				SGSTPercent = item.SGSTPercent,
				SGSTAmount = sgstAmount,
				IGSTPercent = item.IGSTPercent,
				IGSTAmount = igstAmount,
				Total = total,
				NetRate = netRate,
				Status = true
			});
		}

		var newSaleDetails = await SaleReturnData.LoadSaleReturnDetailBySaleReturn(sale.Id);
		var newTotal = newSaleDetails.Sum(d => d.Total) * (1 - (sale.DiscPercent / 100));
		sale.RoundOff = Math.Round(newTotal) - newTotal;

		if (sale.Cash > 0)
			sale.Cash = Math.Round(newTotal);
		if (sale.Card > 0)
			sale.Card = Math.Round(newTotal);
		if (sale.UPI > 0)
			sale.UPI = Math.Round(newTotal);
		if (sale.Credit > 0)
			sale.Credit = Math.Round(newTotal);

		await SaleReturnData.InsertSaleReturn(sale);
	}
}

static async Task UpdateProductLocation()
{
	var products = await CommonData.LoadTableDataByStatus<ProductModel>(TableNames.Product);
	foreach (var product in products)
		await ProductData.InsertProductLocation(new()
		{
			Id = 0,
			ProductId = product.Id,
			LocationId = 48,
			Rate = product.Rate,
			Status = true
		});
}