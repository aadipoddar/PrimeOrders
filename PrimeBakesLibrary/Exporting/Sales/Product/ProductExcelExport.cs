using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Sales.Product;

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
	public static async Task<MemoryStream> ExportProduct<T>(IEnumerable<T> productData)
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
		var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
		{
			// ID - Center aligned, no totals
			[nameof(ProductModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

			// Text fields - Left aligned
			[nameof(ProductModel.Name)] = new() { DisplayName = "Product Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
			[nameof(ProductModel.Code)] = new() { DisplayName = "Product Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
			["Category"] = new() { DisplayName = "Category", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["Tax"] = new() { DisplayName = "Tax", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			[nameof(ProductModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

			// Numeric fields - Right aligned
			[nameof(ProductModel.Rate)] = new() { DisplayName = "Rate", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, Format = "0.00" },

			// Status - Center aligned
			[nameof(ProductModel.Status)] = new() { DisplayName = "Status", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
		};

		// Define column order
		List<string> columnOrder =
		[
			nameof(ProductModel.Id),
			nameof(ProductModel.Name),
			nameof(ProductModel.Code),
			"Category",
			nameof(ProductModel.Rate),
			"Tax",
			nameof(ProductModel.Remarks),
			nameof(ProductModel.Status)
		];

		// Call the generic Excel export utility
		return await ExcelReportExportUtil.ExportToExcel(
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
