using PrimeBakesLibrary.Models.Inventory.Purchase;

namespace PrimeBakesLibrary.Exporting.Inventory.Purchase;

/// <summary>
/// PDF export functionality for Purchase Return Report
/// </summary>
public static class PurchaseReturnReportPdfExport
{
    /// <summary>
    /// Export Purchase Return Report to PDF with custom column order and formatting
    /// </summary>
    /// <param name="purchaseReturnData">Collection of purchase return overview records</param>
    /// <param name="dateRangeStart">Start date of the report</param>
    /// <param name="dateRangeEnd">End date of the report</param>
    /// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
    /// <returns>MemoryStream containing the PDF file</returns>
    public static async Task<MemoryStream> ExportPurchaseReturnReport(
        IEnumerable<PurchaseReturnOverviewModel> purchaseReturnData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true)
    {
        // Define custom column settings matching Excel export
        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>();

        // Define column order based on visibility setting (matching Excel export)
        List<string> columnOrder;

        if (showAllColumns)
            columnOrder =
            [
                nameof(PurchaseReturnOverviewModel.TransactionNo),
                nameof(PurchaseReturnOverviewModel.TransactionDateTime),
                nameof(PurchaseReturnOverviewModel.CompanyName),
                nameof(PurchaseReturnOverviewModel.PartyName),
                nameof(PurchaseReturnOverviewModel.FinancialYear),
                nameof(PurchaseReturnOverviewModel.TotalItems),
                nameof(PurchaseReturnOverviewModel.TotalQuantity),
                nameof(PurchaseReturnOverviewModel.BaseTotal),
                nameof(PurchaseReturnOverviewModel.ItemDiscountAmount),
                nameof(PurchaseReturnOverviewModel.TotalAfterItemDiscount),
                nameof(PurchaseReturnOverviewModel.TotalInclusiveTaxAmount),
                nameof(PurchaseReturnOverviewModel.TotalExtraTaxAmount),
                nameof(PurchaseReturnOverviewModel.TotalAfterTax),
                nameof(PurchaseReturnOverviewModel.OtherChargesPercent),
                nameof(PurchaseReturnOverviewModel.OtherChargesAmount),
                nameof(PurchaseReturnOverviewModel.CashDiscountPercent),
                nameof(PurchaseReturnOverviewModel.CashDiscountAmount),
                nameof(PurchaseReturnOverviewModel.RoundOffAmount),
                nameof(PurchaseReturnOverviewModel.TotalAmount),
                nameof(PurchaseReturnOverviewModel.Remarks),
                nameof(PurchaseReturnOverviewModel.CreatedByName),
                nameof(PurchaseReturnOverviewModel.CreatedAt),
                nameof(PurchaseReturnOverviewModel.CreatedFromPlatform),
                nameof(PurchaseReturnOverviewModel.LastModifiedByUserName),
                nameof(PurchaseReturnOverviewModel.LastModifiedAt),
                nameof(PurchaseReturnOverviewModel.LastModifiedFromPlatform)
            ];
        // Summary columns - key fields only (matching Excel export)
        else
            columnOrder =
            [
                nameof(PurchaseReturnOverviewModel.TransactionNo),
                nameof(PurchaseReturnOverviewModel.TransactionDateTime),
                nameof(PurchaseReturnOverviewModel.PartyName),
                nameof(PurchaseReturnOverviewModel.TotalQuantity),
                nameof(PurchaseReturnOverviewModel.TotalAfterTax),
                nameof(PurchaseReturnOverviewModel.OtherChargesPercent),
                nameof(PurchaseReturnOverviewModel.CashDiscountPercent),
                nameof(PurchaseReturnOverviewModel.TotalAmount)
            ];

        // Customize specific columns for PDF display (matching Excel column names)
        columnSettings[nameof(PurchaseReturnOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", IncludeInTotal = false };
        columnSettings[nameof(PurchaseReturnOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", IncludeInTotal = false };
        columnSettings[nameof(PurchaseReturnOverviewModel.CompanyName)] = new() { DisplayName = "Company", IncludeInTotal = false };
        columnSettings[nameof(PurchaseReturnOverviewModel.PartyName)] = new() { DisplayName = "Party", IncludeInTotal = false };
        columnSettings[nameof(PurchaseReturnOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", IncludeInTotal = false };
        columnSettings[nameof(PurchaseReturnOverviewModel.Remarks)] = new() { DisplayName = "Remarks", IncludeInTotal = false };
        columnSettings[nameof(PurchaseReturnOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", IncludeInTotal = false };
        columnSettings[nameof(PurchaseReturnOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false };
        columnSettings[nameof(PurchaseReturnOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", IncludeInTotal = false };
        columnSettings[nameof(PurchaseReturnOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", IncludeInTotal = false };
        columnSettings[nameof(PurchaseReturnOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false };
        columnSettings[nameof(PurchaseReturnOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", IncludeInTotal = false };

        columnSettings[nameof(PurchaseReturnOverviewModel.TotalItems)] = new()
        {
            DisplayName = "Items",
            Format = "#,##0",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(PurchaseReturnOverviewModel.TotalQuantity)] = new()
        {
            DisplayName = "Qty",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(PurchaseReturnOverviewModel.BaseTotal)] = new()
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

        columnSettings[nameof(PurchaseReturnOverviewModel.ItemDiscountAmount)] = new()
        {
            DisplayName = "Dis Amt",
            Format = "#,##0.00",
            HighlightNegative = true,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(PurchaseReturnOverviewModel.TotalAfterItemDiscount)] = new()
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

        columnSettings[nameof(PurchaseReturnOverviewModel.TotalInclusiveTaxAmount)] = new()
        {
            DisplayName = "Incl Tax",
            Format = "#,##0.00",
            HighlightNegative = true,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(PurchaseReturnOverviewModel.TotalExtraTaxAmount)] = new()
        {
            DisplayName = "Extra Tax",
            Format = "#,##0.00",
            HighlightNegative = true,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(PurchaseReturnOverviewModel.TotalAfterTax)] = new()
        {
            DisplayName = "Sub Total",
            Format = "#,##0.00",
            HighlightNegative = true,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(PurchaseReturnOverviewModel.OtherChargesPercent)] = new()
        {
            DisplayName = "Other Charges %",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            },
            IncludeInTotal = false
        };

        columnSettings[nameof(PurchaseReturnOverviewModel.OtherChargesAmount)] = new()
        {
            DisplayName = "Other Charges Amt",
            Format = "#,##0.00",
            HighlightNegative = true,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(PurchaseReturnOverviewModel.CashDiscountPercent)] = new()
        {
            DisplayName = "Cash Disc %",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            },
            IncludeInTotal = false
        };

        columnSettings[nameof(PurchaseReturnOverviewModel.CashDiscountAmount)] = new()
        {
            DisplayName = "Cash Disc Amt",
            Format = "#,##0.00",
            HighlightNegative = true,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(PurchaseReturnOverviewModel.RoundOffAmount)] = new()
        {
            DisplayName = "Round Off",
            Format = "#,##0.00",
            HighlightNegative = true,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(PurchaseReturnOverviewModel.TotalAmount)] = new()
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

        // Call the generic PDF export utility with landscape mode for all columns
        return await PDFReportExportUtil.ExportToPdf(
            purchaseReturnData,
            "PURCHASE RETURN REPORT",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            useLandscape: showAllColumns  // Use landscape when showing all columns
        );
    }
}
