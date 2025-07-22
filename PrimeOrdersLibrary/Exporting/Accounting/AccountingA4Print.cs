using PrimeOrdersLibrary.Data.Accounts.FinancialAccounting;
using PrimeOrdersLibrary.Data.Common;
using PrimeOrdersLibrary.Models.Accounts.FinancialAccounting;
using PrimeOrdersLibrary.Models.Accounts.Masters;

using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;

namespace PrimeOrdersLibrary.Exporting.Accounting;

public static class AccountingA4Print
{
	public static async Task<MemoryStream> GenerateA4AccountingVoucher(int accountingId)
	{
		// Load main accounting entry
		var accounting = await AccountingData.LoadAccountingOverviewByAccountingId(accountingId);
		if (accounting is null)
			throw new Exception("Accounting entry not found.");

		// Load voucher type
		var voucher = await CommonData.LoadTableDataById<VoucherModel>(TableNames.Voucher, accounting.VoucherId);

		// Load user
		var user = await CommonData.LoadTableDataById<UserModel>(TableNames.User, accounting.UserId);

		// Load ledger details
		var details = await AccountingData.LoadAccountingDetailsByAccounting(accountingId);

		// Prepare ledger display list
		var ledgerRows = new List<AccountingCartModel>();
		foreach (var detail in details)
		{
			var ledger = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, detail.LedgerId);
			ledgerRows.Add(new()
			{
				Serial = ledgerRows.Count + 1,
				Id = ledger.Id,
				Name = ledger.Name,
				Debit = detail.Debit,
				Credit = detail.Credit,
				Remarks = detail.Remarks
			});
		}

		// Create PDF document and page
		var (pdfDocument, pdfPage) = PDFExportUtil.CreateA4Document("Accounting Voucher");

		// Draw company info and voucher header
		float currentY = PDFExportUtil.DrawCompanyInformation(pdfPage, "ACCOUNTING VOUCHER");

		// Draw voucher details section
		currentY = DrawVoucherDetails(pdfPage, currentY, accounting, voucher, user);

		// Draw ledger table
		var result = DrawLedgerTable(pdfPage, currentY, ledgerRows);

		// Draw summary section
		DrawSummary(pdfDocument, result.Page, result.Bounds.Bottom + 20, accounting, ledgerRows);

		// Return PDF as MemoryStream
		return PDFExportUtil.FinalizePdfDocument(pdfDocument);
	}

	private static float DrawVoucherDetails(PdfPage pdfPage, float currentY, AccountingOverviewModel accounting, VoucherModel voucher, UserModel user)
	{
		var leftColumnDetails = new Dictionary<string, string>
		{
			["Ref No"] = accounting.ReferenceNo ?? "N/A",
			["Date"] = accounting.AccountingDate.ToString("dddd, MMMM dd, yyyy"),
			["Voucher"] = voucher?.Name ?? "N/A"
		};

		var rightColumnDetails = new Dictionary<string, string>
		{
			["Fin Year"] = accounting.FinancialYearPeriod,
			["User"] = user?.Name ?? "N/A"
		};

		if (!string.IsNullOrEmpty(accounting.Remarks))
			rightColumnDetails["Remarks"] = accounting.Remarks;

		return PDFExportUtil.DrawInvoiceDetailsSection(pdfPage, currentY, "Voucher Details", leftColumnDetails, rightColumnDetails);
	}

	private static PdfGridLayoutResult DrawLedgerTable(PdfPage pdfPage, float currentY, List<AccountingCartModel> ledgerRows)
	{
		var dataSource = ledgerRows.Select((item, index) => new
		{
			SNo = index + 1,
			Ledger = item.Name,
			Debit = item.Debit > 0 ? item.Debit.FormatIndianCurrency() : "",
			Credit = item.Credit > 0 ? item.Credit.FormatIndianCurrency() : "",
			Remarks = item.Remarks ?? ""
		}).ToList();

		var tableWidth = pdfPage.GetClientSize().Width - PDFExportUtil._pageMargin * 2;
		var columnWidths = new float[]
		{
			tableWidth * 0.08f, // S.No
            tableWidth * 0.38f, // Ledger
            tableWidth * 0.18f, // Debit
            tableWidth * 0.18f, // Credit
            tableWidth * 0.18f  // Remarks
        };

		var columnAlignments = new PdfTextAlignment[]
		{
			PdfTextAlignment.Center, // S.No
            PdfTextAlignment.Left,   // Ledger
            PdfTextAlignment.Right,  // Debit
            PdfTextAlignment.Right,  // Credit
            PdfTextAlignment.Left    // Remarks
        };

		var pdfGrid = PDFExportUtil.CreateStyledGrid(dataSource, columnWidths, columnAlignments);

		var result = pdfGrid.Draw(pdfPage, new RectangleF(PDFExportUtil._pageMargin, currentY, tableWidth,
			pdfPage.GetClientSize().Height - currentY - PDFExportUtil._pageMargin));

		return result;
	}

	private static float DrawSummary(PdfDocument pdfDocument, PdfPage pdfPage, float currentY, AccountingOverviewModel accounting, List<AccountingCartModel> ledgerRows)
	{
		var totalDebit = ledgerRows.Sum(x => x.Debit);
		var totalCredit = ledgerRows.Sum(x => x.Credit);

		var summaryItems = new Dictionary<string, string>
		{
			["Total Debit: "] = totalDebit.FormatIndianCurrency(),
			["Total Credit: "] = totalCredit.FormatIndianCurrency(),
			["Balance: "] = (totalDebit - totalCredit).FormatIndianCurrency()
		};

		if (!string.IsNullOrEmpty(accounting.Remarks))
			summaryItems["Remarks: "] = accounting.Remarks;

		return PDFExportUtil.DrawSummarySection(pdfDocument, pdfPage, currentY, summaryItems, (decimal)(totalDebit - totalCredit));
	}
}