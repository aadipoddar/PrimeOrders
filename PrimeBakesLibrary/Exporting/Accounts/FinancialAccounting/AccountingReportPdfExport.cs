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

        if (showAllColumns)
        {
            // All columns - detailed view
            columnOrder =
            [
                "TransactionNo",
                "TransactionDateTime",
                "CompanyName",
                "VoucherName",
                "ReferenceNo",
                "FinancialYear",
                "TotalDebitLedgers",
                "TotalCreditLedgers",
                "TotalDebitAmount",
                "TotalCreditAmount",
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
            // Summary columns - key fields only
            columnOrder =
            [
                "TransactionNo",
                "TransactionDateTime",
                "CompanyName",
                "VoucherName",
                "TotalDebitAmount",
                "TotalCreditAmount",
                "TotalAmount"
            ];
        }

        // Customize specific columns for PDF display
        columnSettings["TransactionNo"] = new() { DisplayName = "Transaction No", IncludeInTotal = false };
        columnSettings["CompanyName"] = new() { DisplayName = "Company Name", IncludeInTotal = false };
        columnSettings["VoucherName"] = new() { DisplayName = "Voucher Type", IncludeInTotal = false };
        columnSettings["ReferenceNo"] = new() { DisplayName = "Reference No", IncludeInTotal = false };
        columnSettings["FinancialYear"] = new() { DisplayName = "Financial Year", IncludeInTotal = false };
        columnSettings["Remarks"] = new() { DisplayName = "Remarks", IncludeInTotal = false };
        columnSettings["CreatedByName"] = new() { DisplayName = "Created By", IncludeInTotal = false };
        columnSettings["CreatedAt"] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false };
        columnSettings["CreatedFromPlatform"] = new() { DisplayName = "Created Platform", IncludeInTotal = false };
        columnSettings["LastModifiedByUserName"] = new() { DisplayName = "Modified By", IncludeInTotal = false };
        columnSettings["LastModifiedAt"] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false };
        columnSettings["LastModifiedFromPlatform"] = new() { DisplayName = "Modified Platform", IncludeInTotal = false };

        columnSettings["TransactionDateTime"] = new()
        {
            DisplayName = "Transaction Date",
            Format = "dd-MMM-yyyy hh:mm tt",
            IncludeInTotal = false
        }; columnSettings["TotalDebitLedgers"] = new()
        {
            DisplayName = "Debit Ledgers",
            Format = "#,##0",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings["TotalCreditLedgers"] = new()
        {
            DisplayName = "Credit Ledgers",
            Format = "#,##0",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings["TotalDebitAmount"] = new()
        {
            DisplayName = "Total Debit",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings["TotalCreditAmount"] = new()
        {
            DisplayName = "Total Credit",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings["TotalAmount"] = new()
        {
            DisplayName = "Amount",
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
