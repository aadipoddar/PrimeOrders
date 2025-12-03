using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;

namespace PrimeBakesLibrary.Exporting.Accounts.FinancialAccounting;

public static class TrialBalanceExcelExport
{
    public static async Task<MemoryStream> ExportTrialBalance(
        IEnumerable<TrialBalanceModel> trialBalanceData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        string groupName = null,
        string accountTypeName = null)
    {
        // Define column settings with proper formatting
        var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
        {
            // ID fields - Center aligned
            ["LedgerId"] = new() { DisplayName = "Ledger ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            ["GroupId"] = new() { DisplayName = "Group ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            ["AccountTypeId"] = new() { DisplayName = "Account Type ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Text fields - Left aligned
            ["LedgerCode"] = new() { DisplayName = "Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            ["LedgerName"] = new() { DisplayName = "Ledger Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            ["GroupName"] = new() { DisplayName = "Group", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            ["AccountTypeName"] = new() { DisplayName = "Account Type", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },

            // Numeric fields - Right aligned with totals
            ["OpeningDebit"] = new() { DisplayName = "Opening Debit", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            ["OpeningCredit"] = new() { DisplayName = "Opening Credit", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            ["OpeningBalance"] = new() { DisplayName = "Opening Balance", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false },
            ["Debit"] = new() { DisplayName = "Period Debit", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            ["Credit"] = new() { DisplayName = "Period Credit", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            ["ClosingDebit"] = new() { DisplayName = "Closing Debit", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            ["ClosingCredit"] = new() { DisplayName = "Closing Credit", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            ["ClosingBalance"] = new() { DisplayName = "Closing Balance", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false }
        };

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
                "ClosingCredit",
                "LedgerId",
                "GroupId",
                "AccountTypeId"
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

        // Call the generic Excel export utility
        return await ExcelExportUtil.ExportToExcel(
            trialBalanceData,
            "TRIAL BALANCE REPORT",
            "Trial Balance",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            null
        );
    }
}
