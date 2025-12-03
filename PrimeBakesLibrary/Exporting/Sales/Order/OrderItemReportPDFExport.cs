using PrimeBakesLibrary.Models.Sales.Order;

namespace PrimeBakesLibrary.Exporting.Sales.Order;

/// <summary>
/// PDF export functionality for Order Item Report
/// </summary>
public static class OrderItemReportPdfExport
{
    /// <summary>
    /// Export Order Item Report to PDF with custom column order and formatting
    /// </summary>
    /// <param name="orderItemData">Collection of order item overview records</param>
    /// <param name="dateRangeStart">Start date of the report</param>
    /// <param name="dateRangeEnd">End date of the report</param>
    /// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
    /// <param name="showLocation">Whether to include location column (for location ID 1 users)</param>
    /// <param name="locationName">Name of the location for report header</param>
    /// <returns>MemoryStream containing the PDF file</returns>
    public static async Task<MemoryStream> ExportOrderItemReport(
        IEnumerable<OrderItemOverviewModel> orderItemData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        bool showLocation = false,
        string locationName = null)
    {
        // Define custom column settings
        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>();

        // Define column order based on showAllColumns flag (matching Excel export)
        List<string> columnOrder;

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
        else
        {
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
        }

        // Customize specific columns for PDF display
        columnSettings[nameof(OrderItemOverviewModel.ItemName)] = new() { DisplayName = "Item", IncludeInTotal = false };
        columnSettings[nameof(OrderItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", IncludeInTotal = false };
        columnSettings[nameof(OrderItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", IncludeInTotal = false };
        columnSettings[nameof(OrderItemOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", IncludeInTotal = false };
        columnSettings[nameof(OrderItemOverviewModel.SaleTransactionNo)] = new() { DisplayName = "Sale Trans No", IncludeInTotal = false };
        columnSettings[nameof(OrderItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", IncludeInTotal = false };
        columnSettings[nameof(OrderItemOverviewModel.LocationName)] = new() { DisplayName = "Location", IncludeInTotal = false };
        columnSettings[nameof(OrderItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", IncludeInTotal = false };
        columnSettings[nameof(OrderItemOverviewModel.OrderRemarks)] = new() { DisplayName = "Order Remarks", IncludeInTotal = false };
        columnSettings[nameof(OrderItemOverviewModel.Remarks)] = new() { DisplayName = "Item Remarks", IncludeInTotal = false };

        columnSettings[nameof(OrderItemOverviewModel.Quantity)] = new()
        {
            DisplayName = "Qty",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        // Call the generic PDF export utility
        return await PDFReportExportUtil.ExportToPdf(
            orderItemData,
            "ORDER ITEM REPORT",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            useLandscape: showAllColumns,  // Use landscape when showing all columns
            locationName: locationName
        );
    }
}
