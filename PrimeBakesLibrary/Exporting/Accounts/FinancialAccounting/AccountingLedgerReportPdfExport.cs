using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;

namespace PrimeBakesLibrary.Exporting.Accounts.FinancialAccounting;

public static class AccountingLedgerReportPdfExport
{
    public static MemoryStream ExportAccountingLedgerReport(
        IEnumerable<AccountingLedgerOverviewModel> ledgerData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        string companyName = null,
        string ledgerName = null,
        TrialBalanceModel trialBalance = null)
    {
        // Define column order based on view type
        List<string> columnOrder;

        if (showAllColumns)
        {
            // Detailed view - all columns
            columnOrder =
            [
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
            ];
        }
        else
        {
            // Summary view - essential columns only
            columnOrder =
            [
                "LedgerName",
                "TransactionNo",
                "TransactionDateTime",
                "ReferenceNo",
                "Debit",
                "Credit"
            ];
        }

        // Define column settings with proper formatting
        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
        {
            // Text fields
            ["LedgerName"] = new() { DisplayName = "Ledger Name", IncludeInTotal = false },
            ["LedgerCode"] = new() { DisplayName = "Code", IncludeInTotal = false },
            ["AccountTypeName"] = new() { DisplayName = "Account Type", IncludeInTotal = false },
            ["GroupName"] = new() { DisplayName = "Group", IncludeInTotal = false },
            ["TransactionNo"] = new() { DisplayName = "Transaction No", IncludeInTotal = false },
            ["CompanyName"] = new() { DisplayName = "Company", IncludeInTotal = false },
            ["ReferenceType"] = new() { DisplayName = "Ref Type", IncludeInTotal = false },
            ["ReferenceNo"] = new() { DisplayName = "Reference No", IncludeInTotal = false },
            ["AccountingRemarks"] = new() { DisplayName = "Accounting Remarks", IncludeInTotal = false },
            ["Remarks"] = new() { DisplayName = "Ledger Remarks", IncludeInTotal = false },

            // Date fields with formatting
            ["TransactionDateTime"] = new() { DisplayName = "Transaction Date", Format = "dd-MMM-yyyy", IncludeInTotal = false },
            ["ReferenceDateTime"] = new() { DisplayName = "Ref Date", Format = "dd-MMM-yyyy hh:mm tt", IncludeInTotal = false },

            // Numeric fields - Right aligned with totals
            ["Debit"] = new()
            {
                DisplayName = "Debit",
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
                DisplayName = "Credit",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = true
            },

            ["ReferenceAmount"] = new()
            {
                DisplayName = "Ref Amount",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = true
            }
        };

        // Prepare custom summary fields if trial balance is provided
        Dictionary<string, string> customSummaryFields = null;
        if (trialBalance != null)
        {
            customSummaryFields = new Dictionary<string, string>
            {
                ["Opening Balance"] = $"₹ {trialBalance.OpeningBalance:N2}",
                ["Closing Balance"] = $"₹ {trialBalance.ClosingBalance:N2}"
            };
        }

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
            locationName: null,
            partyName: ledgerName,
            customSummaryFields: customSummaryFields
        );
    }
}
