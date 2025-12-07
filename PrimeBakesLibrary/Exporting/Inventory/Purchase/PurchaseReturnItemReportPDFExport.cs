using PrimeBakesLibrary.Models.Inventory.Purchase;

namespace PrimeBakesLibrary.Exporting.Inventory.Purchase;

/// <summary>
/// PDF export functionality for Purchase Return Item Report
/// </summary>
public static class PurchaseReturnItemReportPDFExport
{
    /// <summary>
    /// Export Purchase Return Item Report to PDF with custom column order and formatting
    /// </summary>
    /// <param name="purchaseReturnItemData">Collection of purchase return item overview records</param>
    /// <param name="dateRangeStart">Start date of the report</param>
    /// <param name="dateRangeEnd">End date of the report</param>
    /// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
    /// <param name="showSummary">Whether to show summary grouped by item</param>
    /// <returns>MemoryStream containing the PDF file</returns>
    public static async Task<MemoryStream> ExportPurchaseReturnItemReport(
        IEnumerable<PurchaseReturnItemOverviewModel> purchaseReturnItemData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        bool showSummary = false)
    {
        // Define custom column settings matching Excel export
        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>();

        // Define column order based on showAllColumns and showSummary flags
        List<string> columnOrder;

        // Summary mode - grouped by item with aggregated values
        if (showSummary)
        {
            columnOrder =
            [
                nameof(PurchaseReturnItemOverviewModel.ItemName),
                nameof(PurchaseReturnItemOverviewModel.ItemCode),
                nameof(PurchaseReturnItemOverviewModel.ItemCategoryName),
                nameof(PurchaseReturnItemOverviewModel.Quantity),
                nameof(PurchaseReturnItemOverviewModel.BaseTotal),
                nameof(PurchaseReturnItemOverviewModel.DiscountAmount),
                nameof(PurchaseReturnItemOverviewModel.AfterDiscount),
                nameof(PurchaseReturnItemOverviewModel.SGSTAmount),
                nameof(PurchaseReturnItemOverviewModel.CGSTAmount),
                nameof(PurchaseReturnItemOverviewModel.IGSTAmount),
                nameof(PurchaseReturnItemOverviewModel.TotalTaxAmount),
                nameof(PurchaseReturnItemOverviewModel.Total),
                nameof(PurchaseReturnItemOverviewModel.NetTotal)
            ];
        }
        else if (showAllColumns)
        {
            // All columns - detailed view (matching Excel export)
            columnOrder =
            [
                nameof(PurchaseReturnItemOverviewModel.ItemName),
                nameof(PurchaseReturnItemOverviewModel.ItemCode),
                nameof(PurchaseReturnItemOverviewModel.ItemCategoryName),
                nameof(PurchaseReturnItemOverviewModel.TransactionNo),
                nameof(PurchaseReturnItemOverviewModel.TransactionDateTime),
                nameof(PurchaseReturnItemOverviewModel.CompanyName),
                nameof(PurchaseReturnItemOverviewModel.PartyName),
                nameof(PurchaseReturnItemOverviewModel.Quantity),
                nameof(PurchaseReturnItemOverviewModel.NetRate),
                nameof(PurchaseReturnItemOverviewModel.BaseTotal),
                nameof(PurchaseReturnItemOverviewModel.DiscountPercent),
                nameof(PurchaseReturnItemOverviewModel.DiscountAmount),
                nameof(PurchaseReturnItemOverviewModel.AfterDiscount),
                nameof(PurchaseReturnItemOverviewModel.SGSTPercent),
                nameof(PurchaseReturnItemOverviewModel.SGSTAmount),
                nameof(PurchaseReturnItemOverviewModel.CGSTPercent),
                nameof(PurchaseReturnItemOverviewModel.CGSTAmount),
                nameof(PurchaseReturnItemOverviewModel.IGSTPercent),
                nameof(PurchaseReturnItemOverviewModel.IGSTAmount),
                nameof(PurchaseReturnItemOverviewModel.TotalTaxAmount),
                nameof(PurchaseReturnItemOverviewModel.InclusiveTax),
                nameof(PurchaseReturnItemOverviewModel.Total),
                nameof(PurchaseReturnItemOverviewModel.NetRate),
                nameof(PurchaseReturnItemOverviewModel.NetTotal),
                nameof(PurchaseReturnItemOverviewModel.PurchaseReturnRemarks),
                nameof(PurchaseReturnItemOverviewModel.Remarks)
            ];
        }
        else
        {
            // Summary columns - key fields only (matching Excel export)
            columnOrder =
            [
                nameof(PurchaseReturnItemOverviewModel.ItemName),
                nameof(PurchaseReturnItemOverviewModel.ItemCode),
                nameof(PurchaseReturnItemOverviewModel.TransactionNo),
                nameof(PurchaseReturnItemOverviewModel.TransactionDateTime),
                nameof(PurchaseReturnItemOverviewModel.PartyName),
                nameof(PurchaseReturnItemOverviewModel.Quantity),
                nameof(PurchaseReturnItemOverviewModel.NetRate),
                nameof(PurchaseReturnItemOverviewModel.NetTotal)
            ];
        }

        // Customize specific columns for PDF display (matching Excel column names)
        columnSettings[nameof(PurchaseReturnItemOverviewModel.ItemName)] = new() { DisplayName = "Item", IncludeInTotal = false };
        columnSettings[nameof(PurchaseReturnItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", IncludeInTotal = false };
        columnSettings[nameof(PurchaseReturnItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", IncludeInTotal = false };
        columnSettings[nameof(PurchaseReturnItemOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", IncludeInTotal = false };
        columnSettings[nameof(PurchaseReturnItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false };
        columnSettings[nameof(PurchaseReturnItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", IncludeInTotal = false };
        columnSettings[nameof(PurchaseReturnItemOverviewModel.PartyName)] = new() { DisplayName = "Party", IncludeInTotal = false };
        columnSettings[nameof(PurchaseReturnItemOverviewModel.PurchaseReturnRemarks)] = new() { DisplayName = "Purchase Return Remarks", IncludeInTotal = false };
        columnSettings[nameof(PurchaseReturnItemOverviewModel.Remarks)] = new() { DisplayName = "Item Remarks", IncludeInTotal = false };
        columnSettings[nameof(PurchaseReturnItemOverviewModel.InclusiveTax)] = new() { DisplayName = "Incl Tax", IncludeInTotal = false };

        columnSettings[nameof(PurchaseReturnItemOverviewModel.Quantity)] = new()
        {
            DisplayName = "Qty",
            Format = "#,##0.00",
            HighlightNegative = true,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(PurchaseReturnItemOverviewModel.Rate)] = new()
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

        columnSettings[nameof(PurchaseReturnItemOverviewModel.NetRate)] = new()
        {
            DisplayName = "Net Rate",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            },
            IncludeInTotal = false
        };

        columnSettings[nameof(PurchaseReturnItemOverviewModel.BaseTotal)] = new()
        {
            DisplayName = "Base Total",
            Format = "#,##0.00",
            HighlightNegative = true,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(PurchaseReturnItemOverviewModel.DiscountPercent)] = new()
        {
            DisplayName = "Disc %",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            },
            IncludeInTotal = false
        };

        columnSettings[nameof(PurchaseReturnItemOverviewModel.DiscountAmount)] = new()
        {
            DisplayName = "Disc Amt",
            Format = "#,##0.00",
            HighlightNegative = true,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(PurchaseReturnItemOverviewModel.AfterDiscount)] = new()
        {
            DisplayName = "After Disc",
            Format = "#,##0.00",
            HighlightNegative = true,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(PurchaseReturnItemOverviewModel.SGSTPercent)] = new()
        {
            DisplayName = "SGST %",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            },
            IncludeInTotal = false
        };

        columnSettings[nameof(PurchaseReturnItemOverviewModel.SGSTAmount)] = new()
        {
            DisplayName = "SGST Amt",
            Format = "#,##0.00",
            HighlightNegative = true,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(PurchaseReturnItemOverviewModel.CGSTPercent)] = new()
        {
            DisplayName = "CGST %",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            },
            IncludeInTotal = false
        };

        columnSettings[nameof(PurchaseReturnItemOverviewModel.CGSTAmount)] = new()
        {
            DisplayName = "CGST Amt",
            Format = "#,##0.00",
            HighlightNegative = true,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(PurchaseReturnItemOverviewModel.IGSTPercent)] = new()
        {
            DisplayName = "IGST %",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            },
            IncludeInTotal = false
        };

        columnSettings[nameof(PurchaseReturnItemOverviewModel.IGSTAmount)] = new()
        {
            DisplayName = "IGST Amt",
            Format = "#,##0.00",
            HighlightNegative = true,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(PurchaseReturnItemOverviewModel.TotalTaxAmount)] = new()
        {
            DisplayName = "Tax Amt",
            Format = "#,##0.00",
            HighlightNegative = true,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(PurchaseReturnItemOverviewModel.Total)] = new()
        {
            DisplayName = "Total",
            Format = "#,##0.00",
            HighlightNegative = true,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(PurchaseReturnItemOverviewModel.NetTotal)] = new()
        {
            DisplayName = "Net Total",
            Format = "#,##0.00",
            HighlightNegative = true,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        // Call the generic PDF export utility with landscape mode for all columns
        return await PDFReportExportUtil.ExportToPdf(
            purchaseReturnItemData,
            "PURCHASE RETURN ITEM REPORT",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            useLandscape: showAllColumns || showSummary  // Use landscape when showing all columns
        );
    }
}
