namespace PrimeBakesLibrary.Exporting.Sales.Product;

/// <summary>
/// PDF export functionality for Product
/// </summary>
public static class ProductPDFExport
{
	/// <summary>
	/// Export Product data to PDF with custom column order and formatting
	/// </summary>
	/// <param name="productData">Collection of product records with enriched category and tax information</param>
	/// <returns>MemoryStream containing the PDF file</returns>
	public static MemoryStream ExportProduct<T>(IEnumerable<T> productData)
	{
		// Create enriched data with status and rate formatting
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
				Rate = rate is decimal rateVal ? $"{rateVal:N2}" : "0.00",
				Tax = tax,
				Remarks = remarks,
				Status = status is bool statusBool && statusBool ? "Active" : "Deleted"
			};
		});

		// Define custom column settings
		var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
		{
			// ID - Center aligned
			["Id"] = new()
			{
				DisplayName = "ID",
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				},
				IncludeInTotal = false
			},

			// Name - Left aligned
			["Name"] = new() { DisplayName = "Product Name", IncludeInTotal = false },

			// Code - Left aligned
			["Code"] = new() { DisplayName = "Code", IncludeInTotal = false },

			// Category - Left aligned
			["Category"] = new() { DisplayName = "Category", IncludeInTotal = false },

			// Rate - Right aligned
			["Rate"] = new()
			{
				DisplayName = "Rate",
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				},
				IncludeInTotal = false
			},

			// Tax - Center aligned
			["Tax"] = new()
			{
				DisplayName = "Tax Code",
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				},
				IncludeInTotal = false
			},

			// Status - Center aligned
			["Status"] = new()
			{
				DisplayName = "Status",
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				},
				IncludeInTotal = false
			}
		};

		// Define column order
		List<string> columnOrder =
		[
			"Id", "Name", "Code", "Category", "Rate", "Tax", "Remarks", "Status"
		];

		// Call the generic PDF export utility
		return PDFReportExportUtil.ExportToPdf(
			formattedData,
			"PRODUCT MASTER",
			null,
			null,
			columnSettings,
			columnOrder,
			useLandscape: true
		);
	}
}
