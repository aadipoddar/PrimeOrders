using OfficeOpenXml;

using PrimeOrdersLibrary.Data.Inventory;

FileInfo fileInfo = new(@"C:\Others\item.xlsx");

ExcelPackage.License.SetNonCommercialPersonal("AadiSoft");

using var package = new ExcelPackage(fileInfo);

await package.LoadAsync(fileInfo);

var worksheet = package.Workbook.Worksheets[0];

//while (worksheet.Cells[row, 1].Value != null)
//{
//	var code = worksheet.Cells[row, 1].Value.ToString();
//	var name = worksheet.Cells[row, 2].Value.ToString();
//	var categoryId = worksheet.Cells[row, 3].Value.ToString();
//	var taxId = worksheet.Cells[row, 4].Value.ToString();
//	var price = worksheet.Cells[row, 5].Value.ToString();

//	if (string.IsNullOrWhiteSpace(code) ||
//		string.IsNullOrWhiteSpace(name) ||
//		string.IsNullOrWhiteSpace(categoryId) ||
//		string.IsNullOrWhiteSpace(taxId) ||
//		string.IsNullOrWhiteSpace(price))
//	{
//		Console.WriteLine("Not Inserted Row = " + row);
//		continue;
//	}

//	code = code.Replace(" ", "");

//	Console.WriteLine("Inserting New Product: " + name + " and code " + code);
//	await ProductData.InsertProduct(new()
//	{
//		Id = 0,
//		Code = code,
//		Name = name,
//		ProductCategoryId = int.Parse(categoryId),
//		LocationId = 1,
//		Rate = decimal.Parse(price),
//		TaxId = int.Parse(taxId),
//		Status = true
//	});

//	row++;
//}

await InsertRawMaterial(worksheet);

Console.WriteLine("Finished importing Items.");
Console.ReadLine();

static async Task InsertRawMaterial(ExcelWorksheet worksheet)
{
	int row = 1;

	while (worksheet.Cells[row, 1].Value != null)
	{
		var code = worksheet.Cells[row, 1].Value.ToString();
		var name = worksheet.Cells[row, 2].Value.ToString();

		if (string.IsNullOrWhiteSpace(code) ||
			string.IsNullOrWhiteSpace(name))
		{
			Console.WriteLine("Not Inserted Row = " + row);
			continue;
		}

		code = code.Replace(" ", "");

		Console.WriteLine("Inserting New Raw Material: " + name + " and code " + code);
		await RawMaterialData.InsertRawMaterial(new()
		{
			Id = 0,
			Code = code,
			Name = name,
			MRP = 0,
			RawMaterialCategoryId = 1,
			TaxId = 7,
			Status = true
		});

		row++;
	}
}