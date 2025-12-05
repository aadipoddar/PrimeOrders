using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;

namespace PrimeBakesLibrary.Exporting.Accounts.FinancialAccounting;

/// <summary>
/// Excel export functionality for Financial Accounting Report
/// </summary>
public static class AccountingReportExcelExport
{
    /// <summary>
    /// Export Financial Accounting Report to Excel with custom column order and formatting
    /// </summary>
    /// <param name="accountingData">Collection of accounting overview records</param>
    /// <param name="dateRangeStart">Start date of the report</param>
    /// <param name="dateRangeEnd">End date of the report</param>
    /// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
    /// <param name="isAdmin">Whether the user is admin (to include admin-only columns)</param>
    /// <param name="companyName">Name of the company for report header</param>
    /// <param name="voucherName">Name of the voucher for report header</param>
    /// <returns>MemoryStream containing the Excel file</returns>
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
        var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
        {
            // IDs - Center aligned, no totals
            [nameof(AccountingOverviewModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(AccountingOverviewModel.CompanyId)] = new() { DisplayName = "Company ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(AccountingOverviewModel.VoucherId)] = new() { DisplayName = "Voucher ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(AccountingOverviewModel.FinancialYearId)] = new() { DisplayName = "Financial Year ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(AccountingOverviewModel.CreatedBy)] = new() { DisplayName = "Created By ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(AccountingOverviewModel.LastModifiedBy)] = new() { DisplayName = "Modified By ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Text fields - Left aligned
            [nameof(AccountingOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(AccountingOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(AccountingOverviewModel.VoucherName)] = new() { DisplayName = "Voucher", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(AccountingOverviewModel.ReferenceNo)] = new() { DisplayName = "Ref No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(AccountingOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(AccountingOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(AccountingOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(AccountingOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(AccountingOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(AccountingOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

            // Dates - Center aligned with custom format
            [nameof(AccountingOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            [nameof(AccountingOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            [nameof(AccountingOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },

            // Numeric fields - Right aligned with totals
            [nameof(AccountingOverviewModel.TotalDebitLedgers)] = new() { DisplayName = "Debit Ledgers", Format = "#,##0", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(AccountingOverviewModel.TotalCreditLedgers)] = new() { DisplayName = "Credit Ledgers", Format = "#,##0", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(AccountingOverviewModel.TotalDebitAmount)] = new() { DisplayName = "Debit", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(AccountingOverviewModel.TotalCreditAmount)] = new() { DisplayName = "Credit", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(AccountingOverviewModel.TotalAmount)] = new() { DisplayName = "Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true }
        };

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

        // Build location name for report header
        string reportLocation = null;
        if (!string.IsNullOrEmpty(companyName) && !string.IsNullOrEmpty(voucherName))
            reportLocation = $"{companyName} - {voucherName}";
        else if (!string.IsNullOrEmpty(companyName))
            reportLocation = companyName;
        else if (!string.IsNullOrEmpty(voucherName))
            reportLocation = voucherName;

        // Call the generic Excel export utility
        return await ExcelReportExportUtil.ExportToExcel(
            accountingData,
            "FINANCIAL ACCOUNTING REPORT",
            "Accounting Transactions",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            locationName: reportLocation
        );
    }
}
