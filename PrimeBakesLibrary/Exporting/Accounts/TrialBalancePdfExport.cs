using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;

namespace PrimeBakesLibrary.Exporting.Accounts;

public static class TrialBalancePdfExport
{
	public static MemoryStream ExportTrialBalance(
		IEnumerable<TrialBalanceModel> trialBalanceData,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		string groupName = null,
		string accountTypeName = null)
	{
		// Define column order based on view type
		List<string> columnOrder;

		if (showAllColumns)
		{
			// Detailed view - all columns
			columnOrder =
			[
				"LedgerCode",
				"LedgerName",
				"GroupName",
				"AccountTypeName",
				"OpeningBalance",
				"OpeningDebit",
				"OpeningCredit",
				"Debit",
				"Credit",
				"ClosingBalance",
				"ClosingDebit",
				"ClosingCredit"
			];
		}
		else
		{
			// Summary view - essential columns only
			columnOrder =
			[
				"LedgerName",
				"GroupName",
				"AccountTypeName",
				"OpeningBalance",
				"Debit",
				"Credit",
				"ClosingBalance"
			];
		}

		// Define column settings with proper formatting
		var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
		{
			// Text fields
			["LedgerCode"] = new() { DisplayName = "Code", IncludeInTotal = false },
			["LedgerName"] = new() { DisplayName = "Ledger Name", IncludeInTotal = false },
			["GroupName"] = new() { DisplayName = "Group", IncludeInTotal = false },
			["AccountTypeName"] = new() { DisplayName = "Account Type", IncludeInTotal = false },

			// Numeric fields - Right aligned with totals
			["OpeningDebit"] = new()
			{
				DisplayName = "Opening Debit",
				Format = "#,##0.00",
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				},
				IncludeInTotal = true
			},

			["OpeningCredit"] = new()
			{
				DisplayName = "Opening Credit",
				Format = "#,##0.00",
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				},
				IncludeInTotal = true
			},

			["OpeningBalance"] = new()
			{
				DisplayName = "Opening Balance",
				Format = "#,##0.00",
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				},
				IncludeInTotal = false
			},

			["Debit"] = new()
			{
				DisplayName = "Period Debit",
				Format = "#,##0.00",
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				},
				IncludeInTotal = true
			},

			["Credit"] = new()
			{
				DisplayName = "Period Credit",
				Format = "#,##0.00",
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				},
				IncludeInTotal = true
			},

			["ClosingDebit"] = new()
			{
				DisplayName = "Closing Debit",
				Format = "#,##0.00",
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				},
				IncludeInTotal = true
			},

			["ClosingCredit"] = new()
			{
				DisplayName = "Closing Credit",
				Format = "#,##0.00",
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				},
				IncludeInTotal = true
			},

			["ClosingBalance"] = new()
			{
				DisplayName = "Closing Balance",
				Format = "#,##0.00",
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				},
				IncludeInTotal = false
			}
		};

		// Call the generic PDF export utility
		// Use landscape for detailed view, portrait for summary view
		return PDFReportExportUtil.ExportToPdf(
			trialBalanceData,
			"TRIAL BALANCE REPORT",
			dateRangeStart,
			dateRangeEnd,
			columnSettings,
			columnOrder,
			useBuiltInStyle: false,
			autoAdjustColumnWidth: true,
			logoPath: null,
			useLandscape: showAllColumns,
			locationName: null
		);
	}
}
