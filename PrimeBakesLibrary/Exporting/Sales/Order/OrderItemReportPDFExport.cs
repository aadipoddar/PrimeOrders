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
    public static MemoryStream ExportOrderItemReport(
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
        else
        {
            columnOrder =
            [
                "ItemName",
                "ItemCode",
                "TransactionNo",
                "TransactionDateTime",
                "SaleTransactionNo",
                "Quantity"
            ];
        }

        // Customize specific columns for PDF display
        columnSettings["ItemName"] = new() { DisplayName = "Item Name", IncludeInTotal = false };
        columnSettings["ItemCode"] = new() { DisplayName = "Item Code", IncludeInTotal = false };
        columnSettings["ItemCategoryName"] = new() { DisplayName = "Category", IncludeInTotal = false };
        columnSettings["TransactionNo"] = new() { DisplayName = "Transaction No", IncludeInTotal = false };
        columnSettings["SaleTransactionNo"] = new() { DisplayName = "Sale Transaction No", IncludeInTotal = false };
        columnSettings["CompanyName"] = new() { DisplayName = "Company", IncludeInTotal = false };
        columnSettings["LocationName"] = new() { DisplayName = "Location", IncludeInTotal = false };
        columnSettings["TransactionDateTime"] = new() { DisplayName = "Transaction Date", Format = "dd-MMM-yyyy hh:mm tt", IncludeInTotal = false };
        columnSettings["OrderRemarks"] = new() { DisplayName = "Order Remarks", IncludeInTotal = false };
        columnSettings["Remarks"] = new() { DisplayName = "Item Remarks", IncludeInTotal = false };

        columnSettings["Quantity"] = new()
        {
            DisplayName = "Quantity",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        // Call the generic PDF export utility
        return PDFReportExportUtil.ExportToPdf(
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
