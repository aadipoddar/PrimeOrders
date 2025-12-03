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
        var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
        {
            // IDs - Center aligned, no totals
            ["Id"] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            ["CompanyId"] = new() { DisplayName = "Company ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            ["VoucherId"] = new() { DisplayName = "Voucher ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            ["FinancialYearId"] = new() { DisplayName = "Financial Year ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            ["CreatedBy"] = new() { DisplayName = "Created By ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            ["LastModifiedBy"] = new() { DisplayName = "Modified By ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Text fields - Left aligned
            ["TransactionNo"] = new() { DisplayName = "Transaction No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            ["CompanyName"] = new() { DisplayName = "Company Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            ["VoucherName"] = new() { DisplayName = "Voucher Type", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            ["ReferenceNo"] = new() { DisplayName = "Reference No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            ["FinancialYear"] = new() { DisplayName = "Financial Year", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            ["Remarks"] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            ["CreatedByName"] = new() { DisplayName = "Created By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            ["CreatedFromPlatform"] = new() { DisplayName = "Created Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            ["LastModifiedByUserName"] = new() { DisplayName = "Modified By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            ["LastModifiedFromPlatform"] = new() { DisplayName = "Modified Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

            // Dates - Center aligned with custom format
            ["TransactionDateTime"] = new() { DisplayName = "Transaction Date", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            ["CreatedAt"] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            ["LastModifiedAt"] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },

            // Numeric fields - Right aligned with totals
            ["TotalDebitLedgers"] = new() { DisplayName = "Debit Ledgers", Format = "#,##0", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            ["TotalCreditLedgers"] = new() { DisplayName = "Credit Ledgers", Format = "#,##0", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            ["TotalDebitAmount"] = new() { DisplayName = "Total Debit", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            ["TotalCreditAmount"] = new() { DisplayName = "Total Credit", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            ["TotalAmount"] = new() { DisplayName = "Amount", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true }
        };

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

        // Build location name for report header
        string reportLocation = null;
        if (!string.IsNullOrEmpty(companyName) && !string.IsNullOrEmpty(voucherName))
            reportLocation = $"{companyName} - {voucherName}";
        else if (!string.IsNullOrEmpty(companyName))
            reportLocation = companyName;
        else if (!string.IsNullOrEmpty(voucherName))
            reportLocation = voucherName;

        // Call the generic Excel export utility
        return await ExcelExportUtil.ExportToExcel(
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
