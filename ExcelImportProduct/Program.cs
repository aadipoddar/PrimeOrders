using OfficeOpenXml;

using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory;
using PrimeBakesLibrary.Data.Product;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Product;

// FileInfo fileInfo = new(@"C:\Others\supplier.xlsx");

// ExcelPackage.License.SetNonCommercialPersonal("AadiSoft");

// using var package = new ExcelPackage(fileInfo);

// await package.LoadAsync(fileInfo);

// var worksheet = package.Workbook.Worksheets[0];

// await InsertProducts(worksheet);

// await UpdateProducts();

// await InsertRawMaterial(worksheet);

// await InsertSupplier(worksheet);

// Console.WriteLine("Finished importing Items.");
// Console.ReadLine();

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
			LocationId = product.LocationId,
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
			LocationId = 1,
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