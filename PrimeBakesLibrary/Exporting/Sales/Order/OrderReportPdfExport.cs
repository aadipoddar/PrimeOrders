using PrimeBakesLibrary.Models.Sales.Order;

namespace PrimeBakesLibrary.Exporting.Sales.Order;

/// <summary>
/// PDF export functionality for Order Report
/// </summary>
public static class OrderReportPdfExport
{
    /// <summary>
    /// Export Order Report to PDF with custom column order and formatting
    /// </summary>
    /// <param name="orderData">Collection of order overview records</param>
    /// <param name="dateRangeStart">Start date of the report</param>
    /// <param name="dateRangeEnd">End date of the report</param>
    /// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
    /// <param name="showLocation">Whether to include location column</param>
    /// <param name="locationName">Name of the location for report header</param>
    /// <returns>MemoryStream containing the PDF file</returns>
    public static MemoryStream ExportOrderReport(
        IEnumerable<OrderOverviewModel> orderData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        bool showLocation = false,
        string locationName = null)
    {
        // Define custom column settings matching Excel export
        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>();

        // Define column order based on visibility setting (matching Excel export)
        List<string> columnOrder;

        if (showAllColumns)
        {
            // All columns - detailed view (matching Excel export)
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
            // Summary columns - key fields only (matching Excel export)
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

        // Customize specific columns for PDF display (matching Excel column names)
        columnSettings["TransactionNo"] = new() { DisplayName = "Transaction No", IncludeInTotal = false };
        columnSettings["SaleTransactionNo"] = new() { DisplayName = "Sale Transaction No", IncludeInTotal = false };
        columnSettings["CompanyName"] = new() { DisplayName = "Company Name", IncludeInTotal = false };
        columnSettings["LocationName"] = new() { DisplayName = "Location Name", IncludeInTotal = false };
        columnSettings["TransactionDateTime"] = new() { DisplayName = "Transaction Date", Format = "dd-MMM-yyyy hh:mm tt", IncludeInTotal = false };
        columnSettings["SaleDateTime"] = new() { DisplayName = "Sale Date", Format = "dd-MMM-yyyy hh:mm tt", IncludeInTotal = false };
        columnSettings["FinancialYear"] = new() { DisplayName = "Financial Year", IncludeInTotal = false };
        columnSettings["Remarks"] = new() { DisplayName = "Remarks", IncludeInTotal = false };
        columnSettings["CreatedByName"] = new() { DisplayName = "Created By", IncludeInTotal = false };
        columnSettings["CreatedAt"] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false };
        columnSettings["CreatedFromPlatform"] = new() { DisplayName = "Created Platform", IncludeInTotal = false };
        columnSettings["LastModifiedByUserName"] = new() { DisplayName = "Modified By", IncludeInTotal = false };
        columnSettings["LastModifiedAt"] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false };
        columnSettings["LastModifiedFromPlatform"] = new() { DisplayName = "Modified Platform", IncludeInTotal = false };

        columnSettings["TotalItems"] = new()
        {
            DisplayName = "Total Items",
            Format = "#,##0",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings["TotalQuantity"] = new()
        {
            DisplayName = "Total Quantity",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        // Call the generic PDF export utility with landscape mode for all columns
        return PDFReportExportUtil.ExportToPdf(
            orderData,
            "ORDER REPORT",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            useLandscape: showAllColumns,  // Use landscape when showing all columns
            locationName: locationName
        );
    }
}
