using PrimeBakesLibrary.Models.Sales.Order;

namespace PrimeBakesLibrary.Exporting.Sales.Order;

/// <summary>
/// Consolidated product model for Order Item Report
/// </summary>
public class ConsolidatedOrderItemModel
{
    public string ItemName { get; set; }
    public string ItemCode { get; set; }
    public string ItemCategoryName { get; set; }
    public decimal TotalQuantity { get; set; }
}

/// <summary>
/// Excel export functionality for Consolidated Order Item Report (grouped by product)
/// </summary>
public static class OrderItemConsolidatedExcelExport
{
    /// <summary>
    /// Export Consolidated Order Item Report to Excel - groups by product and sums quantities
    /// </summary>
    /// <param name="orderItemData">Collection of order item overview records</param>
    /// <param name="dateRangeStart">Start date of the report</param>
    /// <param name="dateRangeEnd">End date of the report</param>
    /// <param name="locationName">Name of the location for report header</param>
    /// <returns>MemoryStream containing the Excel file</returns>
    public static async Task<MemoryStream> ExportConsolidatedOrderItemReport(
        IEnumerable<OrderItemOverviewModel> orderItemData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        string locationName = null)
    {
        // Group by item and sum quantities
        var consolidatedData = orderItemData
            .GroupBy(x => new { x.ItemName, x.ItemCode, x.ItemCategoryName })
            .Select(g => new ConsolidatedOrderItemModel
            {
                ItemName = g.Key.ItemName,
                ItemCode = g.Key.ItemCode,
                ItemCategoryName = g.Key.ItemCategoryName,
                TotalQuantity = g.Sum(x => x.Quantity)
            })
            .OrderBy(x => x.ItemName)
            .ToList();

        // Define custom column settings
        var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
        {
            // Text fields
            [nameof(ConsolidatedOrderItemModel.ItemName)] = new() { DisplayName = "Item", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(ConsolidatedOrderItemModel.ItemCode)] = new() { DisplayName = "Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(ConsolidatedOrderItemModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

            // Numeric fields - Quantity
            [nameof(ConsolidatedOrderItemModel.TotalQuantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true }
        };

        // Define column order
        List<string> columnOrder =
        [
            nameof(ConsolidatedOrderItemModel.ItemName),
            nameof(ConsolidatedOrderItemModel.ItemCode),
            nameof(ConsolidatedOrderItemModel.ItemCategoryName),
            nameof(ConsolidatedOrderItemModel.TotalQuantity)
        ];

        // Export using the generic utility
        return await ExcelExportUtil.ExportToExcel(
            consolidatedData,
            "CONSOLIDATED ORDER ITEM REPORT",
            "Order Items Grouped by Product",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            locationName
        );
    }
}
