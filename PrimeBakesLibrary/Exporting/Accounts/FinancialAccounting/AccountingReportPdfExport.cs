using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;

namespace PrimeBakesLibrary.Exporting.Accounts.FinancialAccounting;

/// <summary>
/// PDF export functionality for Financial Accounting Report
/// </summary>
public static class AccountingReportPdfExport
{
    /// <summary>
    /// Export Financial Accounting Report to PDF with custom column order and formatting
    /// </summary>
    /// <param name="accountingData">Collection of accounting overview records</param>
    /// <param name="dateRangeStart">Start date of the report</param>
    /// <param name="dateRangeEnd">End date of the report</param>
    /// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
    /// <param name="isAdmin">Whether the user is admin (to include admin-only columns)</param>
    /// <param name="companyName">Name of the company for report header</param>
    /// <param name="voucherName">Name of the voucher for report header</param>
    /// <returns>MemoryStream containing the PDF file</returns>
    public static async Task<MemoryStream> ExportAccountingReport(
        IEnumerable<AccountingOverviewModel> accountingData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        bool isAdmin = false,
        string companyName = null,
        string voucherName = null)
    {
        // Define custom column settings
        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>();

        // Define column order based on visibility setting
        List<string> columnOrder;

        // All columns - detailed view
        if (showAllColumns)
            columnOrder =
            [
                nameof(AccountingOverviewModel.TransactionNo),
                nameof(AccountingOverviewModel.TransactionDateTime),
                nameof(AccountingOverviewModel.CompanyName),
                nameof(AccountingOverviewModel.VoucherName),
                nameof(AccountingOverviewModel.ReferenceNo),
                nameof(AccountingOverviewModel.FinancialYear),
                nameof(AccountingOverviewModel.TotalDebitLedgers),
                nameof(AccountingOverviewModel.TotalCreditLedgers),
                nameof(AccountingOverviewModel.TotalDebitAmount),
                nameof(AccountingOverviewModel.TotalCreditAmount),
                nameof(AccountingOverviewModel.TotalAmount),
                nameof(AccountingOverviewModel.Remarks),
                nameof(AccountingOverviewModel.CreatedByName),
                nameof(AccountingOverviewModel.CreatedAt),
                nameof(AccountingOverviewModel.CreatedFromPlatform),
                nameof(AccountingOverviewModel.LastModifiedByUserName),
                nameof(AccountingOverviewModel.LastModifiedAt),
                nameof(AccountingOverviewModel.LastModifiedFromPlatform)
            ];
        // Summary columns - key fields only
        else
            columnOrder =
            [
                nameof(AccountingOverviewModel.TransactionNo),
                nameof(AccountingOverviewModel.TransactionDateTime),
                nameof(AccountingOverviewModel.CompanyName),
                nameof(AccountingOverviewModel.VoucherName),
                nameof(AccountingOverviewModel.TotalDebitAmount),
                nameof(AccountingOverviewModel.TotalCreditAmount),
                nameof(AccountingOverviewModel.TotalAmount)
            ];

        // Customize specific columns for PDF display
        columnSettings[nameof(AccountingOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", IncludeInTotal = false };
        columnSettings[nameof(AccountingOverviewModel.CompanyName)] = new() { DisplayName = "Company", IncludeInTotal = false };
        columnSettings[nameof(AccountingOverviewModel.VoucherName)] = new() { DisplayName = "Voucher", IncludeInTotal = false };
        columnSettings[nameof(AccountingOverviewModel.ReferenceNo)] = new() { DisplayName = "Ref No", IncludeInTotal = false };
        columnSettings[nameof(AccountingOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", IncludeInTotal = false };
        columnSettings[nameof(AccountingOverviewModel.Remarks)] = new() { DisplayName = "Remarks", IncludeInTotal = false };
        columnSettings[nameof(AccountingOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", IncludeInTotal = false };
        columnSettings[nameof(AccountingOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false };
        columnSettings[nameof(AccountingOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", IncludeInTotal = false };
        columnSettings[nameof(AccountingOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", IncludeInTotal = false };
        columnSettings[nameof(AccountingOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false };
        columnSettings[nameof(AccountingOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", IncludeInTotal = false };

        columnSettings[nameof(AccountingOverviewModel.TransactionDateTime)] = new()
        {
            DisplayName = "Trans Date",
            Format = "dd-MMM-yyyy hh:mm tt",
            IncludeInTotal = false
        };
        
        columnSettings[nameof(AccountingOverviewModel.TotalDebitLedgers)] = new()
        {
            DisplayName = "Debit Ledgers",
            Format = "#,##0",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(AccountingOverviewModel.TotalCreditLedgers)] = new()
        {
            DisplayName = "Credit Ledgers",
            Format = "#,##0",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(AccountingOverviewModel.TotalDebitAmount)] = new()
        {
            DisplayName = "Debit",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(AccountingOverviewModel.TotalCreditAmount)] = new()
        {
            DisplayName = "Credit",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(AccountingOverviewModel.TotalAmount)] = new()
        {
            DisplayName = "Amt",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        // Build location name for report header
        string reportLocation = null;
        if (!string.IsNullOrEmpty(companyName) && !string.IsNullOrEmpty(voucherName))
            reportLocation = $"{companyName} - {voucherName}";
        else if (!string.IsNullOrEmpty(companyName))
            reportLocation = companyName;
        else if (!string.IsNullOrEmpty(voucherName))
            reportLocation = voucherName;

        // Call the generic PDF export utility
        // Use landscape only when showing all columns, portrait for summary view
        return await PDFReportExportUtil.ExportToPdf(
            accountingData,
            "FINANCIAL ACCOUNTING REPORT",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            useBuiltInStyle: false,
            autoAdjustColumnWidth: true,
            logoPath: null,
            useLandscape: showAllColumns,
            locationName: reportLocation
        );
    }
}
