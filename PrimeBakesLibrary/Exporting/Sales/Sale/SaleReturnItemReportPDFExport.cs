using PrimeBakesLibrary.Models.Sales.Sale;

namespace PrimeBakesLibrary.Exporting.Sales.Sale;

/// <summary>
/// PDF export functionality for Sale Return Item Report
/// </summary>
public static class SaleReturnItemReportPDFExport
{
    /// <summary>
    /// Export Sale Return Item Report to PDF with custom column order and formatting
    /// </summary>
    /// <param name="saleReturnItemData">Collection of sale return item overview records</param>
    /// <param name="dateRangeStart">Start date of the report</param>
    /// <param name="dateRangeEnd">End date of the report</param>
    /// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
    /// <param name="showLocation">Whether to include location column (for location ID 1 users)</param>
    /// <returns>MemoryStream containing the PDF file</returns>
    public static MemoryStream ExportSaleReturnItemReport(
        IEnumerable<SaleReturnItemOverviewModel> saleReturnItemData,
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
            List<string> columns =
            [
                nameof(SaleReturnItemOverviewModel.ItemName),
                nameof(SaleReturnItemOverviewModel.ItemCode),
                nameof(SaleReturnItemOverviewModel.ItemCategoryName),
                nameof(SaleReturnItemOverviewModel.TransactionNo),
                nameof(SaleReturnItemOverviewModel.TransactionDateTime),
                nameof(SaleReturnItemOverviewModel.CompanyName)
            ];

            if (showLocation)
                columns.Add(nameof(SaleReturnItemOverviewModel.LocationName));

            columns.AddRange([
                nameof(SaleReturnItemOverviewModel.PartyName),
                nameof(SaleReturnItemOverviewModel.Quantity),
                nameof(SaleReturnItemOverviewModel.Rate),
                nameof(SaleReturnItemOverviewModel.BaseTotal),
                nameof(SaleReturnItemOverviewModel.DiscountPercent),
                nameof(SaleReturnItemOverviewModel.DiscountAmount),
                nameof(SaleReturnItemOverviewModel.AfterDiscount),
                nameof(SaleReturnItemOverviewModel.SGSTPercent),
                nameof(SaleReturnItemOverviewModel.SGSTAmount),
                nameof(SaleReturnItemOverviewModel.CGSTPercent),
                nameof(SaleReturnItemOverviewModel.CGSTAmount),
                nameof(SaleReturnItemOverviewModel.IGSTPercent),
                nameof(SaleReturnItemOverviewModel.IGSTAmount),
                nameof(SaleReturnItemOverviewModel.TotalTaxAmount),
                nameof(SaleReturnItemOverviewModel.InclusiveTax),
                nameof(SaleReturnItemOverviewModel.Total),
                nameof(SaleReturnItemOverviewModel.NetRate),
                nameof(SaleReturnItemOverviewModel.NetTotal),
				nameof(SaleReturnItemOverviewModel.SaleReturnRemarks),
                nameof(SaleReturnItemOverviewModel.Remarks)
            ]);

            columnOrder = columns;
        }
        // Summary columns - key fields only (matching Excel export)
        else
            columnOrder =
            [
                nameof(SaleReturnItemOverviewModel.ItemName),
                nameof(SaleReturnItemOverviewModel.ItemCode),
                nameof(SaleReturnItemOverviewModel.TransactionNo),
                nameof(SaleReturnItemOverviewModel.TransactionDateTime),
                nameof(SaleReturnItemOverviewModel.LocationName),
                nameof(SaleReturnItemOverviewModel.PartyName),
                nameof(SaleReturnItemOverviewModel.Quantity),
                nameof(SaleReturnItemOverviewModel.NetRate),
                nameof(SaleReturnItemOverviewModel.NetTotal)
            ];

        // Customize specific columns for PDF display (matching Excel column names)
        columnSettings[nameof(SaleReturnItemOverviewModel.ItemName)] = new() { DisplayName = "Item", IncludeInTotal = false };
        columnSettings[nameof(SaleReturnItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", IncludeInTotal = false };
        columnSettings[nameof(SaleReturnItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", IncludeInTotal = false };
        columnSettings[nameof(SaleReturnItemOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", IncludeInTotal = false };
        columnSettings[nameof(SaleReturnItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false };
        columnSettings[nameof(SaleReturnItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", IncludeInTotal = false };
        columnSettings[nameof(SaleReturnItemOverviewModel.LocationName)] = new() { DisplayName = "Location", IncludeInTotal = false };
        columnSettings[nameof(SaleReturnItemOverviewModel.PartyName)] = new() { DisplayName = "Party", IncludeInTotal = false };
        columnSettings[nameof(SaleReturnItemOverviewModel.SaleReturnRemarks)] = new() { DisplayName = "Sale Return Remarks", IncludeInTotal = false };
        columnSettings[nameof(SaleReturnItemOverviewModel.Remarks)] = new() { DisplayName = "Item Remarks", IncludeInTotal = false };
        columnSettings[nameof(SaleReturnItemOverviewModel.InclusiveTax)] = new() { DisplayName = "Incl Tax", IncludeInTotal = false };
        columnSettings[nameof(SaleReturnItemOverviewModel.Quantity)] = new()
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

        columnSettings[nameof(SaleReturnItemOverviewModel.Rate)] = new()
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

        columnSettings[nameof(SaleReturnItemOverviewModel.NetRate)] = new()
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

        columnSettings[nameof(SaleReturnItemOverviewModel.BaseTotal)] = new()
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

        columnSettings[nameof(SaleReturnItemOverviewModel.DiscountPercent)] = new()
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

        columnSettings[nameof(SaleReturnItemOverviewModel.DiscountAmount)] = new()
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

        columnSettings[nameof(SaleReturnItemOverviewModel.AfterDiscount)] = new()
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

        columnSettings[nameof(SaleReturnItemOverviewModel.SGSTPercent)] = new()
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

        columnSettings[nameof(SaleReturnItemOverviewModel.SGSTAmount)] = new()
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

        columnSettings[nameof(SaleReturnItemOverviewModel.CGSTPercent)] = new()
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

        columnSettings[nameof(SaleReturnItemOverviewModel.CGSTAmount)] = new()
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

        columnSettings[nameof(SaleReturnItemOverviewModel.IGSTPercent)] = new()
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

        columnSettings[nameof(SaleReturnItemOverviewModel.IGSTAmount)] = new()
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

        columnSettings[nameof(SaleReturnItemOverviewModel.TotalTaxAmount)] = new()
        {
            DisplayName = "Tax",
            Format = "#,##0.00",
            HighlightNegative = true,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(SaleReturnItemOverviewModel.Total)] = new()
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

        columnSettings[nameof(SaleReturnItemOverviewModel.NetTotal)] = new()
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
		return PDFReportExportUtil.ExportToPdf(
            saleReturnItemData,
            "SALE RETURN ITEM REPORT",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            useLandscape: showAllColumns,  // Use landscape when showing all columns
            locationName: locationName
        );
    }
}
