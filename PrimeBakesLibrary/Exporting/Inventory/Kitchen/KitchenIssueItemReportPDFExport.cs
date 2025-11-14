using PrimeBakesLibrary.Models.Inventory.Kitchen;

namespace PrimeBakesLibrary.Exporting.Inventory.Kitchen;

/// <summary>
/// PDF export functionality for Kitchen Issue Item Report
/// </summary>
public static class KitchenIssueItemReportPDFExport
{
    /// <summary>
    /// Export Kitchen Issue Item Report to PDF with custom column order and formatting
    /// </summary>
    /// <param name="kitchenIssueItemData">Collection of kitchen issue item overview records</param>
    /// <param name="dateRangeStart">Start date of the report</param>
    /// <param name="dateRangeEnd">End date of the report</param>
    /// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
    /// <returns>MemoryStream containing the PDF file</returns>
    public static MemoryStream ExportKitchenIssueItemReport(
        IEnumerable<KitchenIssueItemOverviewModel> kitchenIssueItemData,
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
                "ItemName",
                "ItemCode",
                "ItemCategoryName",
                "TransactionNo",
                "TransactionDateTime",
                "CompanyName",
                "KitchenName",
                "Quantity",
                "Rate",
                "Total",
                "KitchenIssueRemarks",
                "Remarks"
            ];
        }
        else
        {
            // Summary columns - key fields only (matching Excel export)
            columnOrder =
            [
                "ItemName",
                "ItemCode",
                "TransactionNo",
                "TransactionDateTime",
                "KitchenName",
                "Quantity",
                "Rate",
                "Total"
            ];
        }

        // Customize specific columns for PDF display (matching Excel column names)
        columnSettings["ItemName"] = new() { DisplayName = "Item Name", IncludeInTotal = false };
        columnSettings["ItemCode"] = new() { DisplayName = "Item Code", IncludeInTotal = false };
        columnSettings["ItemCategoryName"] = new() { DisplayName = "Category", IncludeInTotal = false };
        columnSettings["TransactionNo"] = new() { DisplayName = "Transaction No", IncludeInTotal = false };
        columnSettings["TransactionDateTime"] = new() { DisplayName = "Transaction Date", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false };
        columnSettings["CompanyName"] = new() { DisplayName = "Company", IncludeInTotal = false };
        columnSettings["KitchenName"] = new() { DisplayName = "Kitchen", IncludeInTotal = false };
        columnSettings["KitchenIssueRemarks"] = new() { DisplayName = "Kitchen Issue Remarks", IncludeInTotal = false };
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

        columnSettings["Rate"] = new()
        {
            DisplayName = "Rate",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            },
            IncludeInTotal = false
        };

        columnSettings["Total"] = new()
        {
            DisplayName = "Total",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        // Call the generic PDF export utility with landscape mode for all columns
        return PDFReportExportUtil.ExportToPdf(
            kitchenIssueItemData,
            "KITCHEN ISSUE ITEM REPORT",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            useLandscape: showAllColumns  // Use landscape when showing all columns
        );
    }
}
