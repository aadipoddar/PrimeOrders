using PrimeBakesLibrary.Models.Sales.Order;

namespace PrimeBakesLibrary.Exporting.Sales.Order;

/// <summary>
/// Excel export functionality for Order Item Report
/// </summary>
public static class OrderItemReportExcelExport
{
    /// <summary>
    /// Export Order Item Report to Excel with custom column order and formatting
    /// </summary>
    /// <param name="orderItemData">Collection of order item overview records</param>
    /// <param name="dateRangeStart">Start date of the report</param>
    /// <param name="dateRangeEnd">End date of the report</param>
    /// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
    /// <param name="showLocation">Whether to include location column (for location ID 1 users)</param>
    /// <param name="locationName">Name of the location for report header</param>
    /// <returns>MemoryStream containing the Excel file</returns>
    public static MemoryStream ExportOrderItemReport(
        IEnumerable<OrderItemOverviewModel> orderItemData,
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
            ["OrderId"] = new() { DisplayName = "Order ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            ["SaleId"] = new() { DisplayName = "Sale ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            ["ItemCategoryId"] = new() { DisplayName = "Category ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            ["CompanyId"] = new() { DisplayName = "Company ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            ["LocationId"] = new() { DisplayName = "Location ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Text fields
            ["ItemName"] = new() { DisplayName = "Item Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            ["ItemCode"] = new() { DisplayName = "Item Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            ["ItemCategoryName"] = new() { DisplayName = "Category", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            ["TransactionNo"] = new() { DisplayName = "Transaction No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            ["SaleTransactionNo"] = new() { DisplayName = "Sale Transaction No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            ["CompanyName"] = new() { DisplayName = "Company", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            ["LocationName"] = new() { DisplayName = "Location", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            ["OrderRemarks"] = new() { DisplayName = "Order Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            ["Remarks"] = new() { DisplayName = "Item Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

            // Date fields
            ["TransactionDateTime"] = new() { DisplayName = "Transaction Date", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },

            // Numeric fields - Quantity
            ["Quantity"] = new() { DisplayName = "Quantity", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true }
        };

        // Define column order based on showAllColumns flag
        List<string> columnOrder;

        // All columns in logical order
        if (showAllColumns)
        {
            List<string> columns =
            [
                "ItemName",
                "ItemCode",
                "ItemCategoryName",
                "TransactionNo",
                "TransactionDateTime",
                "CompanyName"
            ];

            if (showLocation)
                columns.Add("LocationName");

            columns.AddRange([
                "SaleTransactionNo",
                "Quantity",
                "OrderRemarks",
                "Remarks"
            ]);

            columnOrder = columns;
        }
        // Summary columns only
        else
            columnOrder =
            [
                "ItemName",
                "ItemCode",
                "TransactionNo",
                "TransactionDateTime",
                "LocationName",
                "SaleTransactionNo",
                "Quantity"
            ];

        // Export using the generic utility
        return ExcelExportUtil.ExportToExcel(
            orderItemData,
            "ORDER ITEM REPORT",
            "Order Item Transactions",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            locationName
        );
    }
}
