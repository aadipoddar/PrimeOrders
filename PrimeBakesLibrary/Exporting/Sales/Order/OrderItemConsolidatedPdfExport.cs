using PrimeBakesLibrary.Models.Sales.Order;

namespace PrimeBakesLibrary.Exporting.Sales.Order;

/// <summary>
/// PDF export functionality for Consolidated Order Item Report (grouped by product)
/// </summary>
public static class OrderItemConsolidatedPdfExport
{
    /// <summary>
    /// Export Consolidated Order Item Report to PDF - groups by product and sums quantities
    /// </summary>
    /// <param name="orderItemData">Collection of order item overview records</param>
    /// <param name="dateRangeStart">Start date of the report</param>
    /// <param name="dateRangeEnd">End date of the report</param>
    /// <param name="locationName">Name of the location for report header</param>
    /// <returns>MemoryStream containing the PDF file</returns>
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
        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>();

        // Define column order
        List<string> columnOrder =
        [
            nameof(ConsolidatedOrderItemModel.ItemName),
            nameof(ConsolidatedOrderItemModel.ItemCode),
            nameof(ConsolidatedOrderItemModel.ItemCategoryName),
            nameof(ConsolidatedOrderItemModel.TotalQuantity)
        ];

        // Customize specific columns for PDF display
        columnSettings[nameof(ConsolidatedOrderItemModel.ItemName)] = new() { DisplayName = "Item", IncludeInTotal = false };
        columnSettings[nameof(ConsolidatedOrderItemModel.ItemCode)] = new() { DisplayName = "Code", IncludeInTotal = false };
        columnSettings[nameof(ConsolidatedOrderItemModel.ItemCategoryName)] = new() { DisplayName = "Category", IncludeInTotal = false };

        columnSettings[nameof(ConsolidatedOrderItemModel.TotalQuantity)] = new()
        {
            DisplayName = "Qty",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        // Call the generic PDF export utility (portrait mode is fine for 4 columns)
        return await PDFReportExportUtil.ExportToPdf(
            consolidatedData,
            "CONSOLIDATED ORDER ITEM REPORT",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            useLandscape: false,
            locationName: locationName
        );
    }
}
