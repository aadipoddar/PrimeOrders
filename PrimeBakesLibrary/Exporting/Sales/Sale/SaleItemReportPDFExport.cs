using PrimeBakesLibrary.Models.Sales.Sale;

namespace PrimeBakesLibrary.Exporting.Sales.Sale;

/// <summary>
/// PDF export functionality for Sale Item Report
/// </summary>
public static class SaleItemReportPDFExport
{
	/// <summary>
	/// Export Sale Item Report to PDF with custom column order and formatting
	/// </summary>
	/// <param name="saleItemData">Collection of sale item overview records</param>
	/// <param name="dateRangeStart">Start date of the report</param>
	/// <param name="dateRangeEnd">End date of the report</param>
	/// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
	/// <param name="showLocation">Whether to include location column (for location ID 1 users)</param>
	/// <returns>MemoryStream containing the PDF file</returns>
	public static MemoryStream ExportSaleItemReport(
		IEnumerable<SaleItemOverviewModel> saleItemData,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showLocation = false,
		string locationName = null)
	{
		// Define custom column settings matching Excel export
		var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>();

		// Define column order based on visibility setting (matching Excel export)
		List<string> columnOrder;

		if (showAllColumns)
		{
			// All columns - detailed view (matching Excel export)
			List<string> columns =
			[
				nameof(SaleItemOverviewModel.ItemName),
				nameof(SaleItemOverviewModel.ItemCode),
				nameof(SaleItemOverviewModel.ItemCategoryName),
				nameof(SaleItemOverviewModel.TransactionNo),
				nameof(SaleItemOverviewModel.TransactionDateTime),
				nameof(SaleItemOverviewModel.CompanyName)
			];

			if (showLocation)
				columns.Add(nameof(SaleItemOverviewModel.LocationName));

			columns.AddRange([
				nameof(SaleItemOverviewModel.PartyName),
				nameof(SaleItemOverviewModel.Quantity),
				nameof(SaleItemOverviewModel.Rate),
				nameof(SaleItemOverviewModel.BaseTotal),
				nameof(SaleItemOverviewModel.DiscountPercent),
				nameof(SaleItemOverviewModel.DiscountAmount),
				nameof(SaleItemOverviewModel.AfterDiscount),
				nameof(SaleItemOverviewModel.SGSTPercent),
				nameof(SaleItemOverviewModel.SGSTAmount),
				nameof(SaleItemOverviewModel.CGSTPercent),
				nameof(SaleItemOverviewModel.CGSTAmount),
				nameof(SaleItemOverviewModel.IGSTPercent),
				nameof(SaleItemOverviewModel.IGSTAmount),
				nameof(SaleItemOverviewModel.TotalTaxAmount),
				nameof(SaleItemOverviewModel.InclusiveTax),
				nameof(SaleItemOverviewModel.Total),
				nameof(SaleItemOverviewModel.NetRate),
				nameof(SaleItemOverviewModel.NetTotal),
				nameof(SaleItemOverviewModel.SaleRemarks),
				nameof(SaleItemOverviewModel.Remarks)
			]);

			columnOrder = columns;
		}
		// Summary columns - key fields only (matching Excel export)
		else
			columnOrder =
			[
				nameof(SaleItemOverviewModel.ItemName),
				nameof(SaleItemOverviewModel.ItemCode),
				nameof(SaleItemOverviewModel.TransactionNo),
				nameof(SaleItemOverviewModel.TransactionDateTime),
				nameof(SaleItemOverviewModel.LocationName),
				nameof(SaleItemOverviewModel.PartyName),
				nameof(SaleItemOverviewModel.Quantity),
				nameof(SaleItemOverviewModel.NetRate),
				nameof(SaleItemOverviewModel.NetTotal)
			];

		// Customize specific columns for PDF display (matching Excel column names)
		columnSettings[nameof(SaleItemOverviewModel.ItemName)] = new() { DisplayName = "Item", IncludeInTotal = false };
		columnSettings[nameof(SaleItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", IncludeInTotal = false };
		columnSettings[nameof(SaleItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", IncludeInTotal = false };
		columnSettings[nameof(SaleItemOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", IncludeInTotal = false };
		columnSettings[nameof(SaleItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false };
		columnSettings[nameof(SaleItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", IncludeInTotal = false };
		columnSettings[nameof(SaleItemOverviewModel.LocationName)] = new() { DisplayName = "Location", IncludeInTotal = false };
		columnSettings[nameof(SaleItemOverviewModel.PartyName)] = new() { DisplayName = "Party", IncludeInTotal = false };
		columnSettings[nameof(SaleItemOverviewModel.SaleRemarks)] = new() { DisplayName = "Sale Remarks", IncludeInTotal = false };
		columnSettings[nameof(SaleItemOverviewModel.Remarks)] = new() { DisplayName = "Item Remarks", IncludeInTotal = false };
		columnSettings[nameof(SaleItemOverviewModel.InclusiveTax)] = new() { DisplayName = "Incl Tax", IncludeInTotal = false };

		columnSettings[nameof(SaleItemOverviewModel.Quantity)] = new()
		{
			DisplayName = "Qty",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings[nameof(SaleItemOverviewModel.Rate)] = new()
		{
			DisplayName = "Rate",
			Format = "#,##0.00",
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			},
			IncludeInTotal = false
		};

		columnSettings[nameof(SaleItemOverviewModel.NetRate)] = new()
		{
			DisplayName = "Net Rate",
			Format = "#,##0.00",
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			},
			IncludeInTotal = false
		};

		columnSettings[nameof(SaleItemOverviewModel.BaseTotal)] = new()
		{
			DisplayName = "Base Total",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings[nameof(SaleItemOverviewModel.DiscountPercent)] = new()
		{
			DisplayName = "Disc %",
			Format = "#,##0.00",
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			},
			IncludeInTotal = false
		};

		columnSettings[nameof(SaleItemOverviewModel.DiscountAmount)] = new()
		{
			DisplayName = "Disc Amt",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings[nameof(SaleItemOverviewModel.AfterDiscount)] = new()
		{
			DisplayName = "After Disc",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings[nameof(SaleItemOverviewModel.SGSTPercent)] = new()
		{
			DisplayName = "SGST %",
			Format = "#,##0.00",
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			},
			IncludeInTotal = false
		};

		columnSettings[nameof(SaleItemOverviewModel.SGSTAmount)] = new()
		{
			DisplayName = "SGST Amt",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings[nameof(SaleItemOverviewModel.CGSTPercent)] = new()
		{
			DisplayName = "CGST %",
			Format = "#,##0.00",
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			},
			IncludeInTotal = false
		};

		columnSettings[nameof(SaleItemOverviewModel.CGSTAmount)] = new()
		{
			DisplayName = "CGST Amt",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings[nameof(SaleItemOverviewModel.IGSTPercent)] = new()
		{
			DisplayName = "IGST %",
			Format = "#,##0.00",
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			},
			IncludeInTotal = false
		};

		columnSettings[nameof(SaleItemOverviewModel.IGSTAmount)] = new()
		{
			DisplayName = "IGST Amt",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings[nameof(SaleItemOverviewModel.TotalTaxAmount)] = new()
		{
			DisplayName = "Tax",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings[nameof(SaleItemOverviewModel.Total)] = new()
		{
			DisplayName = "Total",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings[nameof(SaleItemOverviewModel.NetTotal)] = new()
		{
			DisplayName = "Net Total",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		// Call the generic PDF export utility with landscape mode for all columns
		return PDFReportExportUtil.ExportToPdf(
			saleItemData,
			"SALE ITEM REPORT",
			dateRangeStart,
			dateRangeEnd,
			columnSettings,
			columnOrder,
			useLandscape: showAllColumns,  // Use landscape when showing all columns
			locationName: locationName
		);
	}
}
