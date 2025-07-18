
using OfficeOpenXml;

using PrimeOrdersLibrary.Data.Common;
using PrimeOrdersLibrary.Data.Inventory;
using PrimeOrdersLibrary.Data.Inventory.Purchase;
using PrimeOrdersLibrary.Data.Product;
using PrimeOrdersLibrary.DataAccess;
using PrimeOrdersLibrary.Models.Product;

FileInfo fileInfo = new(@"C:\Others\supplier.xlsx");

ExcelPackage.License.SetNonCommercialPersonal("AadiSoft");

using var package = new ExcelPackage(fileInfo);

//await package.LoadAsync(fileInfo);

//var worksheet = package.Workbook.Worksheets[0];

// await InsertProducts(worksheet);

await UpdateProducts();

// await InsertRawMaterial(worksheet);

// await InsertSupplier(worksheet);

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
		var code = worksheet.Cells[row, 1].Value.ToString();
		var name = worksheet.Cells[row, 2].Value.ToString();
		var categoryId = worksheet.Cells[row, 3].Value.ToString();
		var taxId = worksheet.Cells[row, 4].Value.ToString();
		var price = worksheet.Cells[row, 5].Value.ToString();

		if (string.IsNullOrWhiteSpace(code) ||
			string.IsNullOrWhiteSpace(name) ||
			string.IsNullOrWhiteSpace(categoryId) ||
			string.IsNullOrWhiteSpace(taxId) ||
			string.IsNullOrWhiteSpace(price))
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
			Rate = decimal.Parse(price),
			TaxId = int.Parse(taxId),
			Status = true
		});

		row++;
	}
}

static async Task InsertSupplier(ExcelWorksheet worksheet)
{
	int row = 1;

	while (worksheet.Cells[row, 1].Value != null)
	{
		var code = worksheet.Cells[row, 1].Value.ToString();
		var name = worksheet.Cells[row, 2].Value.ToString();
		var address = worksheet.Cells[row, 3].Value?.ToString() ?? string.Empty;
		var phone = worksheet.Cells[row, 4].Value?.ToString() ?? string.Empty;
		var gstNo = worksheet.Cells[row, 5].Value?.ToString() ?? string.Empty;

		if (string.IsNullOrWhiteSpace(code) ||
			string.IsNullOrWhiteSpace(name))
		{
			Console.WriteLine("Not Inserted Row = " + row);
			continue;
		}

		code = code.Replace(" ", "");

		Console.WriteLine("Inserting New Product: " + name + " and code " + code);
		await SupplierData.InsertSupplier(new()
		{
			Id = 0,
			Code = code,
			Name = name,
			Address = address ?? string.Empty,
			Email = string.Empty,
			GSTNo = gstNo ?? string.Empty,
			Phone = phone ?? string.Empty,
			StateId = 2,
			LocationId = null,
			Status = true
		});

		row++;
	}
}