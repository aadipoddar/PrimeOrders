using PrimeBakesLibrary.Models.Sales.Sale;

namespace PrimeBakesLibrary.Exporting.Sales.Sale;

/// <summary>
/// PDF export functionality for Sale Return Report
/// </summary>
public static class SaleReturnReportPdfExport
{
    /// <summary>
    /// Export Sale Return Report to PDF with custom column order and formatting
    /// </summary>
    /// <param name="saleReturnData">Collection of sale return overview records</param>
    /// <param name="dateRangeStart">Start date of the report</param>
    /// <param name="dateRangeEnd">End date of the report</param>
    /// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
    /// <param name="showLocation">Whether to include location column</param>
    /// <param name="locationName">Name of the location for report header</param>
    /// <returns>MemoryStream containing the PDF file</returns>
    public static MemoryStream ExportSaleReturnReport(
        IEnumerable<SaleReturnOverviewModel> saleReturnData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        bool showLocation = false,
        string locationName = null,
        string partyName = null)
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
                nameof(SaleReturnOverviewModel.TransactionNo),
                nameof(SaleReturnOverviewModel.CompanyName)
            ];

            // Add location columns if showLocation is true
            if (showLocation)
                columnOrder.Add(nameof(SaleReturnOverviewModel.LocationName));

            // Continue with remaining columns
            columnOrder.AddRange(
            [
                nameof(SaleReturnOverviewModel.PartyName),
                nameof(SaleReturnOverviewModel.CustomerName),
                nameof(SaleReturnOverviewModel.TransactionDateTime),
                nameof(SaleReturnOverviewModel.FinancialYear),
                nameof(SaleReturnOverviewModel.TotalItems),
                nameof(SaleReturnOverviewModel.TotalQuantity),
                nameof(SaleReturnOverviewModel.BaseTotal),
                nameof(SaleReturnOverviewModel.ItemDiscountAmount),
                nameof(SaleReturnOverviewModel.TotalAfterItemDiscount),
                nameof(SaleReturnOverviewModel.TotalInclusiveTaxAmount),
                nameof(SaleReturnOverviewModel.TotalExtraTaxAmount),
                nameof(SaleReturnOverviewModel.TotalAfterTax),
                nameof(SaleReturnOverviewModel.OtherChargesPercent),
                nameof(SaleReturnOverviewModel.OtherChargesAmount),
                nameof(SaleReturnOverviewModel.DiscountPercent),
                nameof(SaleReturnOverviewModel.DiscountAmount),
                nameof(SaleReturnOverviewModel.RoundOffAmount),
                nameof(SaleReturnOverviewModel.TotalAmount),
                nameof(SaleReturnOverviewModel.Cash),
                nameof(SaleReturnOverviewModel.Card),
                nameof(SaleReturnOverviewModel.UPI),
                nameof(SaleReturnOverviewModel.Credit),
                nameof(SaleReturnOverviewModel.PaymentModes),
                nameof(SaleReturnOverviewModel.Remarks),
                nameof(SaleReturnOverviewModel.CreatedByName),
                nameof(SaleReturnOverviewModel.CreatedAt),
                nameof(SaleReturnOverviewModel.CreatedFromPlatform),
                nameof(SaleReturnOverviewModel.LastModifiedByUserName),
                nameof(SaleReturnOverviewModel.LastModifiedAt),
                nameof(SaleReturnOverviewModel.LastModifiedFromPlatform)
            ]);
        }
        else
        {
            // Summary columns - key fields only (matching Excel export)
            columnOrder =
            [
                nameof(SaleReturnOverviewModel.TransactionNo),
                nameof(SaleReturnOverviewModel.TransactionDateTime),
                nameof(SaleReturnOverviewModel.TotalQuantity),
                nameof(SaleReturnOverviewModel.TotalAfterTax),
                nameof(SaleReturnOverviewModel.DiscountPercent),
                nameof(SaleReturnOverviewModel.DiscountAmount),
                nameof(SaleReturnOverviewModel.TotalAmount),
                nameof(SaleReturnOverviewModel.PaymentModes)
            ];

            // Add location column only if not showing location in header
            if (!showLocation)
                columnOrder.Insert(3, nameof(SaleReturnOverviewModel.LocationName));

            // Add party column only if not showing party in header
            if (string.IsNullOrEmpty(partyName))
            {
                int insertIndex = showLocation ? 3 : 4;
                columnOrder.Insert(insertIndex, nameof(SaleReturnOverviewModel.PartyName));
            }
        }

        // Customize specific columns for PDF display (matching Excel column names)
        columnSettings[nameof(SaleReturnOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", IncludeInTotal = false };
        columnSettings[nameof(SaleReturnOverviewModel.CompanyName)] = new() { DisplayName = "Company", IncludeInTotal = false };
        columnSettings[nameof(SaleReturnOverviewModel.LocationName)] = new() { DisplayName = "Location", IncludeInTotal = false };
        columnSettings[nameof(SaleReturnOverviewModel.PartyName)] = new() { DisplayName = "Party", IncludeInTotal = false };
        columnSettings[nameof(SaleReturnOverviewModel.CustomerName)] = new() { DisplayName = "Customer", IncludeInTotal = false };
        columnSettings[nameof(SaleReturnOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", IncludeInTotal = false };
        columnSettings[nameof(SaleReturnOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", IncludeInTotal = false };
        columnSettings[nameof(SaleReturnOverviewModel.Remarks)] = new() { DisplayName = "Remarks", IncludeInTotal = false };
        columnSettings[nameof(SaleReturnOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", IncludeInTotal = false };
        columnSettings[nameof(SaleReturnOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false };
        columnSettings[nameof(SaleReturnOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", IncludeInTotal = false };
        columnSettings[nameof(SaleReturnOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", IncludeInTotal = false };
        columnSettings[nameof(SaleReturnOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false };
        columnSettings[nameof(SaleReturnOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", IncludeInTotal = false };
        columnSettings[nameof(SaleReturnOverviewModel.TotalItems)] = new()
        {
            DisplayName = "Items",
            Format = "#,##0",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(SaleReturnOverviewModel.TotalQuantity)] = new()
        {
            DisplayName = "Qty",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(SaleReturnOverviewModel.BaseTotal)] = new()
        {
            DisplayName = "Base Total",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(SaleReturnOverviewModel.ItemDiscountAmount)] = new()
        {
            DisplayName = "Item Discount Amount",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(SaleReturnOverviewModel.TotalAfterItemDiscount)] = new()
        {
            DisplayName = "After Disc",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(SaleReturnOverviewModel.TotalInclusiveTaxAmount)] = new()
        {
            DisplayName = "Incl Tax",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(SaleReturnOverviewModel.TotalExtraTaxAmount)] = new()
        {
            DisplayName = "Extra Tax",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(SaleReturnOverviewModel.TotalAfterTax)] = new()
        {
            DisplayName = "Sub Total",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(SaleReturnOverviewModel.OtherChargesPercent)] = new()
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

        columnSettings[nameof(SaleReturnOverviewModel.OtherChargesAmount)] = new()
        {
            DisplayName = "Other Charges",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(SaleReturnOverviewModel.DiscountPercent)] = new()
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

        columnSettings[nameof(SaleReturnOverviewModel.DiscountAmount)] = new()
        {
            DisplayName = "Disc Amt",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(SaleReturnOverviewModel.RoundOffAmount)] = new()
        {
            DisplayName = "Round Off",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(SaleReturnOverviewModel.TotalAmount)] = new()
        {
            DisplayName = "Total",
            Format = "#,##0.00",
            IsRequired = true,
            IsGrandTotal = true,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(SaleReturnOverviewModel.Cash)] = new()
        {
            DisplayName = "Cash",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(SaleReturnOverviewModel.Card)] = new()
        {
            DisplayName = "Card",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(SaleReturnOverviewModel.UPI)] = new()
        {
            DisplayName = "UPI",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(SaleReturnOverviewModel.Credit)] = new()
        {
            DisplayName = "Credit",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(SaleReturnOverviewModel.PaymentModes)] = new()
        {
            DisplayName = "Payment Modes",
            IncludeInTotal = false
        };

        // Call the generic PDF export utility with landscape mode for all columns
        return PDFReportExportUtil.ExportToPdf(
            saleReturnData,
            "SALE RETURN REPORT",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            useLandscape: showAllColumns,  // Use landscape when showing all columns
            locationName: locationName
        );
    }
}
