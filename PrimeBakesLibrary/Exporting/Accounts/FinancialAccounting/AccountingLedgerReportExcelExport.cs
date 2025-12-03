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
            [nameof(AccountingLedgerOverviewModel.Id)] = new() { DisplayName = "Ledger ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(AccountingLedgerOverviewModel.MasterId)] = new() { DisplayName = "Accounting ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(AccountingLedgerOverviewModel.AccountTypeId)] = new() { DisplayName = "Account Type ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(AccountingLedgerOverviewModel.GroupId)] = new() { DisplayName = "Group ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(AccountingLedgerOverviewModel.CompanyId)] = new() { DisplayName = "Company ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(AccountingLedgerOverviewModel.ReferenceId)] = new() { DisplayName = "Reference ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Text fields - Left aligned
            [nameof(AccountingLedgerOverviewModel.LedgerName)] = new() { DisplayName = "Ledger", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(AccountingLedgerOverviewModel.LedgerCode)] = new() { DisplayName = "Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(AccountingLedgerOverviewModel.AccountTypeName)] = new() { DisplayName = "Account Type", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(AccountingLedgerOverviewModel.GroupName)] = new() { DisplayName = "Group", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(AccountingLedgerOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(AccountingLedgerOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(AccountingLedgerOverviewModel.ReferenceType)] = new() { DisplayName = "Ref Type", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(AccountingLedgerOverviewModel.ReferenceNo)] = new() { DisplayName = "Ref No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(AccountingLedgerOverviewModel.AccountingRemarks)] = new() { DisplayName = "Accounting Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(AccountingLedgerOverviewModel.Remarks)] = new() { DisplayName = "Ledger Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },

            // Date fields - Center aligned
            [nameof(AccountingLedgerOverviewModel.TransactionDateTime)] = new() { DisplayName = "Transaction Date", Format = "dd/MM/yyyy", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(AccountingLedgerOverviewModel.ReferenceDateTime)] = new() { DisplayName = "Reference Date", Format = "dd/MM/yyyy hh:mm tt", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Numeric fields - Right aligned with totals
            [nameof(AccountingLedgerOverviewModel.Debit)] = new() { DisplayName = "Debit", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(AccountingLedgerOverviewModel.Credit)] = new() { DisplayName = "Credit", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(AccountingLedgerOverviewModel.ReferenceAmount)] = new() { DisplayName = "Ref Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true }
        };

        // Define column order based on view type
        List<string> columnOrder;

        // Detailed view - all columns
        if (showAllColumns)
            columnOrder =
            [
                nameof(AccountingLedgerOverviewModel.LedgerName),
                nameof(AccountingLedgerOverviewModel.LedgerCode),
                nameof(AccountingLedgerOverviewModel.AccountTypeName),
                nameof(AccountingLedgerOverviewModel.GroupName),
                nameof(AccountingLedgerOverviewModel.TransactionNo),
                nameof(AccountingLedgerOverviewModel.TransactionDateTime),
                nameof(AccountingLedgerOverviewModel.CompanyName),
                nameof(AccountingLedgerOverviewModel.ReferenceType),
                nameof(AccountingLedgerOverviewModel.ReferenceNo),
                nameof(AccountingLedgerOverviewModel.ReferenceDateTime),
                nameof(AccountingLedgerOverviewModel.ReferenceAmount),
                nameof(AccountingLedgerOverviewModel.Debit),
                nameof(AccountingLedgerOverviewModel.Credit),
                nameof(AccountingLedgerOverviewModel.AccountingRemarks),
                nameof(AccountingLedgerOverviewModel.Remarks)
            ];
        // Summary view - essential columns only
        else
            columnOrder =
            [
                nameof(AccountingLedgerOverviewModel.LedgerName),
                nameof(AccountingLedgerOverviewModel.TransactionNo),
                nameof(AccountingLedgerOverviewModel.TransactionDateTime),
                nameof(AccountingLedgerOverviewModel.ReferenceNo),
                nameof(AccountingLedgerOverviewModel.Debit),
                nameof(AccountingLedgerOverviewModel.Credit)
            ];

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
