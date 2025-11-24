using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;

namespace PrimeBakesLibrary.Exporting.Accounts;

public static class AccountingLedgerReportPdfExport
{
    public static MemoryStream ExportAccountingLedgerReport(
        IEnumerable<AccountingLedgerOverviewModel> ledgerData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        string companyName = null,
        string ledgerName = null)
    {
        // Define column order based on view type
        List<string> columnOrder;

        if (showAllColumns)
        {
            // Detailed view - all columns
            columnOrder = new()
            {
                "LedgerName",
                "LedgerCode",
                "AccountTypeName",
                "GroupName",
                "TransactionNo",
                "TransactionDateTime",
                "CompanyName",
                "ReferenceType",
                "ReferenceNo",
                "ReferenceDateTime",
                "ReferenceAmount",
                "Debit",
                "Credit",
                "AccountingRemarks",
                "Remarks"
            };
        }
        else
        {
            // Summary view - essential columns only
            columnOrder = new()
            {
                "LedgerName",
                "TransactionNo",
                "TransactionDateTime",
                "CompanyName",
                "ReferenceNo",
                "Debit",
                "Credit"
            };
        }

        // Define column settings with proper formatting
        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>();

        // Text fields
        columnSettings["LedgerName"] = new() { DisplayName = "Ledger Name", IncludeInTotal = false };
        columnSettings["LedgerCode"] = new() { DisplayName = "Code", IncludeInTotal = false };
        columnSettings["AccountTypeName"] = new() { DisplayName = "Account Type", IncludeInTotal = false };
        columnSettings["GroupName"] = new() { DisplayName = "Group", IncludeInTotal = false };
        columnSettings["TransactionNo"] = new() { DisplayName = "Transaction No", IncludeInTotal = false };
        columnSettings["CompanyName"] = new() { DisplayName = "Company", IncludeInTotal = false };
        columnSettings["ReferenceType"] = new() { DisplayName = "Ref Type", IncludeInTotal = false };
        columnSettings["ReferenceNo"] = new() { DisplayName = "Reference No", IncludeInTotal = false };
        columnSettings["AccountingRemarks"] = new() { DisplayName = "Accounting Remarks", IncludeInTotal = false };
        columnSettings["Remarks"] = new() { DisplayName = "Ledger Remarks", IncludeInTotal = false };

        // Date fields with formatting
        columnSettings["TransactionDateTime"] = new() { DisplayName = "Transaction Date", Format = "dd-MMM-yyyy", IncludeInTotal = false };
        columnSettings["ReferenceDateTime"] = new() { DisplayName = "Ref Date", Format = "dd-MMM-yyyy hh:mm tt", IncludeInTotal = false };

        // Numeric fields - Right aligned with totals
        columnSettings["Debit"] = new()
        {
            DisplayName = "Debit",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            },
            IncludeInTotal = true
        };

        columnSettings["Credit"] = new()
        {
            DisplayName = "Credit",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            },
            IncludeInTotal = true
        };

        columnSettings["ReferenceAmount"] = new()
        {
            DisplayName = "Ref Amount",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            },
            IncludeInTotal = true
        };

        // Build report location text
        string reportLocation = "FINANCIAL LEDGER REPORT";
        if (!string.IsNullOrWhiteSpace(companyName))
            reportLocation += $" - {companyName}";
        if (!string.IsNullOrWhiteSpace(ledgerName))
            reportLocation += $" - {ledgerName}";

        // Call the generic PDF export utility
        // Use landscape only when showing all columns, portrait for summary view
        return PDFReportExportUtil.ExportToPdf(
            ledgerData,
            "FINANCIAL LEDGER REPORT",
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
