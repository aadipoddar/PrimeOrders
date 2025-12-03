using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;

namespace PrimeBakesLibrary.Exporting.Accounts.FinancialAccounting;

public static class AccountingLedgerReportExcelExport
{
    public static async Task<MemoryStream> ExportAccountingLedgerReport(
        IEnumerable<AccountingLedgerOverviewModel> ledgerData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        string companyName = null,
        string ledgerName = null,
        TrialBalanceModel trialBalance = null)
    {
        // Define column settings with proper formatting
        var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
        {
            // ID fields - Center aligned
            ["Id"] = new() { DisplayName = "Ledger ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            ["AccountingId"] = new() { DisplayName = "Accounting ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            ["AccountTypeId"] = new() { DisplayName = "Account Type ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            ["GroupId"] = new() { DisplayName = "Group ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            ["CompanyId"] = new() { DisplayName = "Company ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            ["ReferenceId"] = new() { DisplayName = "Reference ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Text fields - Left aligned
            ["LedgerName"] = new() { DisplayName = "Ledger Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            ["LedgerCode"] = new() { DisplayName = "Ledger Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            ["AccountTypeName"] = new() { DisplayName = "Account Type", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            ["GroupName"] = new() { DisplayName = "Group", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            ["TransactionNo"] = new() { DisplayName = "Transaction No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            ["CompanyName"] = new() { DisplayName = "Company", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            ["ReferenceType"] = new() { DisplayName = "Reference Type", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            ["ReferenceNo"] = new() { DisplayName = "Reference No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            ["AccountingRemarks"] = new() { DisplayName = "Accounting Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            ["Remarks"] = new() { DisplayName = "Ledger Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },

            // Date fields - Center aligned
            ["TransactionDateTime"] = new() { DisplayName = "Transaction Date", Format = "dd/MM/yyyy", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            ["ReferenceDateTime"] = new() { DisplayName = "Reference Date", Format = "dd/MM/yyyy hh:mm tt", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Numeric fields - Right aligned with totals
            ["Debit"] = new() { DisplayName = "Debit", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            ["Credit"] = new() { DisplayName = "Credit", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            ["ReferenceAmount"] = new() { DisplayName = "Reference Amount", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true }
        };

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
                "Remarks",
                "Id",
                "AccountingId",
                "AccountTypeId",
                "GroupId",
                "CompanyId",
                "ReferenceId"
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

        // Call the generic Excel export utility
        return await ExcelExportUtil.ExportToExcel(
            ledgerData,
            "FINANCIAL LEDGER REPORT",
            "Ledger Report",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            null,
            ledgerName,
            customSummaryFields
        );
    }
}
