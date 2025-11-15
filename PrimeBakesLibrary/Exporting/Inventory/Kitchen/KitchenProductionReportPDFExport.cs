using PrimeBakesLibrary.Models.Inventory.Kitchen;

namespace PrimeBakesLibrary.Exporting.Inventory.Kitchen;

/// <summary>
/// PDF export functionality for Kitchen Production Report
/// </summary>
public static class KitchenProductionReportPDFExport
{
    /// <summary>
    /// Export Kitchen Production Report to PDF with custom column order and formatting
    /// </summary>
    /// <param name="kitchenProductionData">Collection of kitchen production overview records</param>
    /// <param name="dateRangeStart">Start date of the report</param>
    /// <param name="dateRangeEnd">End date of the report</param>
    /// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
    /// <returns>MemoryStream containing the PDF file</returns>
    public static MemoryStream ExportKitchenProductionReport(
        IEnumerable<KitchenProductionOverviewModel> kitchenProductionData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true)
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
                "TransactionDateTime",
                "CompanyName",
                "KitchenName",
                "FinancialYear",
                "TotalItems",
                "TotalQuantity",
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
            // Summary columns - key fields only (matching Excel export)
            columnOrder =
            [
                "TransactionNo",
                "TransactionDateTime",
                "KitchenName",
                "TotalQuantity",
                "TotalAmount"
            ];
        }

        // Customize specific columns for PDF display (matching Excel column names)
        columnSettings["TransactionNo"] = new() { DisplayName = "Transaction No", IncludeInTotal = false };
        columnSettings["TransactionDateTime"] = new() { DisplayName = "Transaction Date", Format = "dd-MMM-yyyy hh:mm tt", IncludeInTotal = false };
        columnSettings["CompanyName"] = new() { DisplayName = "Company", IncludeInTotal = false };
        columnSettings["KitchenName"] = new() { DisplayName = "Kitchen", IncludeInTotal = false };
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

        columnSettings["TotalAmount"] = new()
        {
            DisplayName = "Total Amount",
            Format = "#,##0.00",
            HighlightNegative = true,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        // Call the generic PDF export utility with landscape mode for all columns
        return PDFReportExportUtil.ExportToPdf(
            kitchenProductionData,
            "KITCHEN PRODUCTION REPORT",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            useLandscape: showAllColumns  // Use landscape when showing all columns
        );
    }
}
