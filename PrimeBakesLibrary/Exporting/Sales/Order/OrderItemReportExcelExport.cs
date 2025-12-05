using PrimeBakesLibrary.Exporting.Utils;
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
    public static async Task<MemoryStream> ExportOrderItemReport(
        IEnumerable<OrderItemOverviewModel> orderItemData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        bool showLocation = false,
        string locationName = null)
    {
        // Define custom column settings
        var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
        {
            // IDs - Center aligned, no totals
            [nameof(OrderItemOverviewModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(OrderItemOverviewModel.MasterId)] = new() { DisplayName = "Master ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(OrderItemOverviewModel.SaleId)] = new() { DisplayName = "Sale ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(OrderItemOverviewModel.ItemCategoryId)] = new() { DisplayName = "Category ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(OrderItemOverviewModel.CompanyId)] = new() { DisplayName = "Company ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(OrderItemOverviewModel.LocationId)] = new() { DisplayName = "Location ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Text fields
            [nameof(OrderItemOverviewModel.ItemName)] = new() { DisplayName = "Item", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(OrderItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(OrderItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(OrderItemOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(OrderItemOverviewModel.SaleTransactionNo)] = new() { DisplayName = "Sale Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(OrderItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(OrderItemOverviewModel.LocationName)] = new() { DisplayName = "Location", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(OrderItemOverviewModel.OrderRemarks)] = new() { DisplayName = "Order Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(OrderItemOverviewModel.Remarks)] = new() { DisplayName = "Item Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

            // Date fields
            [nameof(OrderItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },

            // Numeric fields - Quantity
            [nameof(OrderItemOverviewModel.Quantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true }
        };

        // Define column order based on showAllColumns flag
        List<string> columnOrder;

        // All columns in logical order
        if (showAllColumns)
        {
            List<string> columns =
            [
                nameof(OrderItemOverviewModel.ItemName),
                nameof(OrderItemOverviewModel.ItemCode),
                nameof(OrderItemOverviewModel.ItemCategoryName),
                nameof(OrderItemOverviewModel.TransactionNo),
                nameof(OrderItemOverviewModel.TransactionDateTime),
                nameof(OrderItemOverviewModel.CompanyName)
            ];

            if (showLocation)
                columns.Add(nameof(OrderItemOverviewModel.LocationName));

            columns.AddRange([
                nameof(OrderItemOverviewModel.SaleTransactionNo),
                nameof(OrderItemOverviewModel.Quantity),
                nameof(OrderItemOverviewModel.OrderRemarks),
                nameof(OrderItemOverviewModel.Remarks)
            ]);

            columnOrder = columns;
        }
        // Summary columns only
        else
            columnOrder =
            [
                nameof(OrderItemOverviewModel.ItemName),
                nameof(OrderItemOverviewModel.ItemCode),
                nameof(OrderItemOverviewModel.TransactionNo),
                nameof(OrderItemOverviewModel.TransactionDateTime),
                nameof(OrderItemOverviewModel.LocationName),
                nameof(OrderItemOverviewModel.SaleTransactionNo),
                nameof(OrderItemOverviewModel.Quantity)
            ];

        // Export using the generic utility
        return await ExcelReportExportUtil.ExportToExcel(
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
