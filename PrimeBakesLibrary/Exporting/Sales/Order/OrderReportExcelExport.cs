using PrimeBakesLibrary.Models.Sales.Order;

namespace PrimeBakesLibrary.Exporting.Sales.Order;

/// <summary>
/// Excel export functionality for Order Report
/// </summary>
public static class OrderReportExcelExport
{
    /// <summary>
    /// Export Order Report to Excel with custom column order and formatting
    /// </summary>
    /// <param name="orderData">Collection of order overview records</param>
    /// <param name="dateRangeStart">Start date of the report</param>
    /// <param name="dateRangeEnd">End date of the report</param>
    /// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
    /// <param name="showLocation">Whether to include location column</param>
    /// <param name="locationName">Name of the location for report header</param>
    /// <returns>MemoryStream containing the Excel file</returns>
    public static MemoryStream ExportOrderReport(
        IEnumerable<OrderOverviewModel> orderData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        bool showLocation = false,
        string locationName = null)
    {
        // Define custom column settings
        var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
        {
            // IDs - Center aligned, no totals
            ["Id"] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            ["CompanyId"] = new() { DisplayName = "Company ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            ["LocationId"] = new() { DisplayName = "Location ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            ["SaleId"] = new() { DisplayName = "Sale ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            ["FinancialYearId"] = new() { DisplayName = "Financial Year ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            ["CreatedBy"] = new() { DisplayName = "Created By ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            ["LastModifiedBy"] = new() { DisplayName = "Modified By ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Text fields - Left aligned
            ["TransactionNo"] = new() { DisplayName = "Transaction No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            ["SaleTransactionNo"] = new() { DisplayName = "Sale Transaction No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            ["CompanyName"] = new() { DisplayName = "Company Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            ["LocationName"] = new() { DisplayName = "Location Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            ["FinancialYear"] = new() { DisplayName = "Financial Year", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            ["Remarks"] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            ["CreatedByName"] = new() { DisplayName = "Created By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            ["CreatedFromPlatform"] = new() { DisplayName = "Created Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            ["LastModifiedByUserName"] = new() { DisplayName = "Modified By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            ["LastModifiedFromPlatform"] = new() { DisplayName = "Modified Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

            // Dates - Center aligned with custom format
            ["TransactionDateTime"] = new() { DisplayName = "Transaction Date", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            ["SaleDateTime"] = new() { DisplayName = "Sale Date", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            ["CreatedAt"] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            ["LastModifiedAt"] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },

            // Numeric fields - Right aligned with totals
            ["TotalItems"] = new() { DisplayName = "Total Items", Format = "#,##0", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            ["TotalQuantity"] = new() { DisplayName = "Total Quantity", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true }
        };

        // Define column order based on visibility setting
        List<string> columnOrder;

        if (showAllColumns)
        {
            // All columns - detailed view
            columnOrder =
            [
                "TransactionNo",
                "SaleTransactionNo",
                "CompanyName"
            ];

            // Add location columns if showLocation is true
            if (showLocation)
            {
                columnOrder.Add("LocationName");
            }

            // Continue with remaining columns
            columnOrder.AddRange(
            [
                "TransactionDateTime",
                "SaleDateTime",
                "FinancialYear",
                "TotalItems",
                "TotalQuantity",
                "Remarks",
                "CreatedByName",
                "CreatedAt",
                "CreatedFromPlatform",
                "LastModifiedByUserName",
                "LastModifiedAt",
                "LastModifiedFromPlatform"
            ]);
        }
        else
        {
            // Summary columns - key fields only
            columnOrder =
            [
                "TransactionNo",
                "SaleTransactionNo",
                "TransactionDateTime"
            ];

            // Add location name if showLocation is true
            if (showLocation)
            {
                columnOrder.Add("LocationName");
            }

            // Continue with remaining summary columns
            columnOrder.AddRange(
            [
                "TotalItems",
                "TotalQuantity"
            ]);
        }

        // Call the generic Excel export utility
        return ExcelExportUtil.ExportToExcel(
            orderData,
            "ORDER REPORT",
            "Order Transactions",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            locationName: locationName
        );
    }
}
