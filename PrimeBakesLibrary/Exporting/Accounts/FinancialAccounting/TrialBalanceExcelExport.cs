using PrimeBakesLibrary.Exporting.Utils;
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
        var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
        {
            // ID fields - Center aligned
            [nameof(TrialBalanceModel.LedgerId)] = new() { DisplayName = "Ledger ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(TrialBalanceModel.GroupId)] = new() { DisplayName = "Group ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(TrialBalanceModel.AccountTypeId)] = new() { DisplayName = "Account Type ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Text fields - Left aligned
            [nameof(TrialBalanceModel.LedgerCode)] = new() { DisplayName = "Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(TrialBalanceModel.LedgerName)] = new() { DisplayName = "Ledger Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(TrialBalanceModel.GroupName)] = new() { DisplayName = "Group", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(TrialBalanceModel.AccountTypeName)] = new() { DisplayName = "Account Type", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },

            // Numeric fields - Right aligned with totals
            [nameof(TrialBalanceModel.OpeningDebit)] = new() { DisplayName = "Opening Debit", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(TrialBalanceModel.OpeningCredit)] = new() { DisplayName = "Opening Credit", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(TrialBalanceModel.OpeningBalance)] = new() { DisplayName = "Opening Balance", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(TrialBalanceModel.Debit)] = new() { DisplayName = "Period Debit", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(TrialBalanceModel.Credit)] = new() { DisplayName = "Period Credit", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(TrialBalanceModel.ClosingDebit)] = new() { DisplayName = "Closing Debit", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(TrialBalanceModel.ClosingCredit)] = new() { DisplayName = "Closing Credit", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(TrialBalanceModel.ClosingBalance)] = new() { DisplayName = "Closing Balance", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true }
        };

        // Define column order based on view type
        List<string> columnOrder;

        // Detailed view - all columns
        if (showAllColumns)
            columnOrder =
            [
                nameof(TrialBalanceModel.LedgerCode),
                nameof(TrialBalanceModel.LedgerName),
                nameof(TrialBalanceModel.GroupName),
                nameof(TrialBalanceModel.AccountTypeName),
                nameof(TrialBalanceModel.OpeningBalance),
                nameof(TrialBalanceModel.OpeningDebit),
                nameof(TrialBalanceModel.OpeningCredit),
                nameof(TrialBalanceModel.Debit),
                nameof(TrialBalanceModel.Credit),
                nameof(TrialBalanceModel.ClosingBalance),
                nameof(TrialBalanceModel.ClosingDebit),
                nameof(TrialBalanceModel.ClosingCredit),
            ];
        // Summary view - essential columns only
        else
            columnOrder =
            [
                nameof(TrialBalanceModel.LedgerName),
                nameof(TrialBalanceModel.GroupName),
                nameof(TrialBalanceModel.AccountTypeName),
                nameof(TrialBalanceModel.OpeningBalance),
                nameof(TrialBalanceModel.Debit),
                nameof(TrialBalanceModel.Credit),
                nameof(TrialBalanceModel.ClosingBalance)
            ];

        // Call the generic Excel export utility
        return await ExcelReportExportUtil.ExportToExcel(
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
