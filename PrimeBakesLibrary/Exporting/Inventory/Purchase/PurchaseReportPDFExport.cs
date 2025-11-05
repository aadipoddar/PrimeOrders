using PrimeBakesLibrary.Models.Inventory.Purchase;

namespace PrimeBakesLibrary.Exporting.Inventory.Purchase;

/// <summary>
/// PDF export functionality for Purchase Report
/// </summary>
public static class PurchaseReportPDFExport
{
	/// <summary>
	/// Export Purchase Report to PDF with custom column order and formatting
	/// </summary>
	/// <param name="purchaseData">Collection of purchase overview records</param>
	/// <param name="dateRangeStart">Start date of the report</param>
	/// <param name="dateRangeEnd">End date of the report</param>
	/// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
	/// <returns>MemoryStream containing the PDF file</returns>
	public static MemoryStream ExportPurchaseReport(
		IEnumerable<PurchaseOverviewModel> purchaseData,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true)
	{
		// Define custom column settings matching Excel export
		var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>();

		// Define column order based on visibility setting (matching Excel export)
		List<string> columnOrder;

		if (showAllColumns)
		{
			// All columns - detailed view (matching Excel export)
			columnOrder =
			[
				"TransactionNo",
				"TransactionDateTime",
				"CompanyName",
				"PartyName",
				"FinancialYear",
				"TotalItems",
				"TotalQuantity",
				"BaseTotal",
				"DiscountPercent",
				"DiscountAmount",
				"AfterDiscount",
				"SGSTPercent",
				"SGSTAmount",
				"CGSTPercent",
				"CGSTAmount",
				"IGSTPercent",
				"IGSTAmount",
				"TotalTaxAmount",
				"TotalAfterTax",
				"OtherChargesPercent",
				"OtherChargesAmount",
				"TotalAfterOtherCharges",
				"CashDiscountPercent",
				"CashDiscountAmount",
				"TotalAfterCashDiscount",
				"RoundOffAmount",
				"TotalAmount",
				"Remarks",
				"CreatedByName",
				"CreatedAt",
				"CreatedFromPlatform",
				"LastModifiedByUserName",
				"LastModifiedAt",
				"LastModifiedFromPlatform"
			];
		}
		else
		{
			// Summary columns - key fields only (matching Excel export)
			columnOrder =
			[
				"TransactionNo",
				"TransactionDateTime",
				"PartyName",
				"TotalQuantity",
				"TotalAfterTax",
				"OtherChargesPercent",
				"CashDiscountPercent",
				"TotalAmount"
			];
		}

		// Customize specific columns for PDF display (matching Excel column names)
		columnSettings["TransactionNo"] = new() { DisplayName = "Transaction No", IncludeInTotal = false };
		columnSettings["TransactionDateTime"] = new() { DisplayName = "Transaction Date", Format = "dd-MMM-yyyy hh:mm tt", IncludeInTotal = false };
		columnSettings["CompanyName"] = new() { DisplayName = "Company", IncludeInTotal = false };
		columnSettings["PartyName"] = new() { DisplayName = "Party", IncludeInTotal = false };
		columnSettings["FinancialYear"] = new() { DisplayName = "Financial Year", IncludeInTotal = false };
		columnSettings["Remarks"] = new() { DisplayName = "Remarks", IncludeInTotal = false };
		columnSettings["CreatedByName"] = new() { DisplayName = "Created By", IncludeInTotal = false };
		columnSettings["CreatedAt"] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false };
		columnSettings["CreatedFromPlatform"] = new() { DisplayName = "Created Platform", IncludeInTotal = false };
		columnSettings["LastModifiedByUserName"] = new() { DisplayName = "Modified By", IncludeInTotal = false };
		columnSettings["LastModifiedAt"] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false };
		columnSettings["LastModifiedFromPlatform"] = new() { DisplayName = "Modified Platform", IncludeInTotal = false };

		columnSettings["TotalItems"] = new()
		{
			DisplayName = "Total Items",
			Format = "#,##0",
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings["TotalQuantity"] = new()
		{
			DisplayName = "Total Quantity",
			Format = "#,##0.00",
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings["BaseTotal"] = new()
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

		columnSettings["DiscountPercent"] = new()
		{
			DisplayName = "Discount %",
			Format = "#,##0.00",
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			},
			IncludeInTotal = false
		};

		columnSettings["DiscountAmount"] = new()
		{
			DisplayName = "Discount Amount",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings["AfterDiscount"] = new()
		{
			DisplayName = "After Discount",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings["SGSTPercent"] = new()
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

		columnSettings["SGSTAmount"] = new()
		{
			DisplayName = "SGST Amount",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings["CGSTPercent"] = new()
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

		columnSettings["CGSTAmount"] = new()
		{
			DisplayName = "CGST Amount",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings["IGSTPercent"] = new()
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

		columnSettings["IGSTAmount"] = new()
		{
			DisplayName = "IGST Amount",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings["TotalTaxAmount"] = new()
		{
			DisplayName = "Total Tax Amount",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings["TotalAfterTax"] = new()
		{
			DisplayName = "Sub Total",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings["OtherChargesPercent"] = new()
		{
			DisplayName = "Other Charges %",
			Format = "#,##0.00",
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			},
			IncludeInTotal = false
		};

		columnSettings["OtherChargesAmount"] = new()
		{
			DisplayName = "Other Charges Amount",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings["TotalAfterOtherCharges"] = new()
		{
			DisplayName = "After Other Charges",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings["CashDiscountPercent"] = new()
		{
			DisplayName = "Cash Discount %",
			Format = "#,##0.00",
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			},
			IncludeInTotal = false
		};

		columnSettings["CashDiscountAmount"] = new()
		{
			DisplayName = "Cash Discount Amount",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings["TotalAfterCashDiscount"] = new()
		{
			DisplayName = "After Cash Discount",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings["RoundOffAmount"] = new()
		{
			DisplayName = "Round Off Amount",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings["TotalAmount"] = new()
		{
			DisplayName = "Total Amount",
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
			purchaseData,
			"PURCHASE REPORT",
			dateRangeStart,
			dateRangeEnd,
			columnSettings,
			columnOrder,
			useLandscape: showAllColumns  // Use landscape when showing all columns
		);
	}
}
