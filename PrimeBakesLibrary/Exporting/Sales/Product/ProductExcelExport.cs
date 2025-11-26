namespace PrimeBakesLibrary.Exporting.Sales.Product;

/// <summary>
/// Excel export functionality for Product
/// </summary>
public static class ProductExcelExport
{
	/// <summary>
	/// Export Product data to Excel with custom column order and formatting
	/// </summary>
	/// <param name="productData">Collection of product records with enriched category and tax information</param>
	/// <returns>MemoryStream containing the Excel file</returns>
	public static MemoryStream ExportProduct<T>(IEnumerable<T> productData)
	{
		// Create enriched data with status formatting
		var formattedData = productData.Select(product =>
		{
			var props = typeof(T).GetProperties();
			var id = props.FirstOrDefault(p => p.Name == "Id")?.GetValue(product);
			var name = props.FirstOrDefault(p => p.Name == "Name")?.GetValue(product)?.ToString();
			var code = props.FirstOrDefault(p => p.Name == "Code")?.GetValue(product)?.ToString();
			var category = props.FirstOrDefault(p => p.Name == "Category")?.GetValue(product)?.ToString();
			var rate = props.FirstOrDefault(p => p.Name == "Rate")?.GetValue(product);
			var tax = props.FirstOrDefault(p => p.Name == "Tax")?.GetValue(product)?.ToString();
			var remarks = props.FirstOrDefault(p => p.Name == "Remarks")?.GetValue(product)?.ToString();
			var status = props.FirstOrDefault(p => p.Name == "Status")?.GetValue(product);

			return new
			{
				Id = id,
				Name = name,
				Code = code,
				Category = category,
				Rate = rate,
				Tax = tax,
				Remarks = remarks,
				Status = status is bool statusBool && statusBool ? "Active" : "Deleted"
			};
		});

		// Define custom column settings
		var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
		{
			// ID - Center aligned, no totals
			["Id"] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

			// Text fields - Left aligned
			["Name"] = new() { DisplayName = "Product Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
			["Code"] = new() { DisplayName = "Product Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
			["Category"] = new() { DisplayName = "Category", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["Tax"] = new() { DisplayName = "Tax", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["Remarks"] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

			// Numeric fields - Right aligned
			["Rate"] = new() { DisplayName = "Rate", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, Format = "0.00" },

			// Status - Center aligned
			["Status"] = new() { DisplayName = "Status", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
		};

		// Define column order
		List<string> columnOrder =
		[
			"Id", "Name", "Code", "Category", "Rate", "Tax", "Remarks", "Status"
		];

		// Call the generic Excel export utility
		return ExcelExportUtil.ExportToExcel(
			formattedData,
			"PRODUCT MASTER",
			"Product Data",
			null,
			null,
			columnSettings,
			columnOrder
		);
	}
}
