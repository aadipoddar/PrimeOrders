using PrimeBakesLibrary.Models.Sales.Sale;

namespace PrimeBakesLibrary.Exporting.Sales.Sale;

/// <summary>
/// PDF export functionality for Sale Report
/// </summary>
public static class SaleReportPdfExport
{
    /// <summary>
    /// Export Sale Report to PDF with custom column order and formatting
    /// </summary>
    /// <param name="saleData">Collection of sale overview records</param>
    /// <param name="dateRangeStart">Start date of the report</param>
    /// <param name="dateRangeEnd">End date of the report</param>
    /// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
    /// <param name="locationName">Name of the location for report header</param>
    /// <param name="partyName">Name of the party for report header</param>
    /// <param name="showSummary">Whether to show summary view with grouped data</param>
    /// <returns>MemoryStream containing the PDF file</returns>
    public static async Task<MemoryStream> ExportSaleReport(
        IEnumerable<SaleOverviewModel> saleData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        string locationName = null,
        string partyName = null,
        bool showSummary = false)
    {
        // Define custom column settings matching Excel export
        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>();

        // Define column order based on visibility setting (matching Excel export)
        List<string> columnOrder;

        // Summary view - grouped by location with totals
        if (showSummary)
            columnOrder =
            [
                nameof(SaleOverviewModel.LocationName),
                nameof(SaleOverviewModel.TotalItems),
                nameof(SaleOverviewModel.TotalQuantity),
                nameof(SaleOverviewModel.BaseTotal),
                nameof(SaleOverviewModel.ItemDiscountAmount),
                nameof(SaleOverviewModel.TotalAfterItemDiscount),
                nameof(SaleOverviewModel.TotalInclusiveTaxAmount),
                nameof(SaleOverviewModel.TotalExtraTaxAmount),
                nameof(SaleOverviewModel.TotalAfterTax),
                nameof(SaleOverviewModel.OtherChargesAmount),
                nameof(SaleOverviewModel.DiscountAmount),
                nameof(SaleOverviewModel.RoundOffAmount),
                nameof(SaleOverviewModel.TotalAmount),
                nameof(SaleOverviewModel.Cash),
                nameof(SaleOverviewModel.Card),
                nameof(SaleOverviewModel.UPI),
                nameof(SaleOverviewModel.Credit)
            ];

        else if (showAllColumns)
        {
            // All columns - detailed view (matching Excel export)
            columnOrder =
            [
                nameof(SaleOverviewModel.TransactionNo),
                nameof(SaleOverviewModel.OrderTransactionNo),
                nameof(SaleOverviewModel.CompanyName)
            ];

            // Add location columns if showLocation is true
            if (string.IsNullOrEmpty(locationName))
                columnOrder.Add(nameof(SaleOverviewModel.LocationName));

            if (string.IsNullOrEmpty(partyName))
                columnOrder.Add(nameof(SaleOverviewModel.PartyName));

			// Continue with remaining columns
			columnOrder.AddRange(
            [
                nameof(SaleOverviewModel.CustomerName),
                nameof(SaleOverviewModel.TransactionDateTime),
                nameof(SaleOverviewModel.FinancialYear),
                nameof(SaleOverviewModel.TotalItems),
                nameof(SaleOverviewModel.TotalQuantity),
                nameof(SaleOverviewModel.BaseTotal),
                nameof(SaleOverviewModel.ItemDiscountAmount),
                nameof(SaleOverviewModel.TotalAfterItemDiscount),
                nameof(SaleOverviewModel.TotalInclusiveTaxAmount),
                nameof(SaleOverviewModel.TotalExtraTaxAmount),
                nameof(SaleOverviewModel.TotalAfterTax),
                nameof(SaleOverviewModel.OtherChargesPercent),
                nameof(SaleOverviewModel.OtherChargesAmount),
                nameof(SaleOverviewModel.DiscountPercent),
                nameof(SaleOverviewModel.DiscountAmount),
                nameof(SaleOverviewModel.RoundOffAmount),
                nameof(SaleOverviewModel.TotalAmount),
                nameof(SaleOverviewModel.Cash),
                nameof(SaleOverviewModel.Card),
                nameof(SaleOverviewModel.UPI),
                nameof(SaleOverviewModel.Credit),
                nameof(SaleOverviewModel.PaymentModes),
                nameof(SaleOverviewModel.Remarks),
                nameof(SaleOverviewModel.CreatedByName),
                nameof(SaleOverviewModel.CreatedAt),
                nameof(SaleOverviewModel.CreatedFromPlatform),
                nameof(SaleOverviewModel.LastModifiedByUserName),
                nameof(SaleOverviewModel.LastModifiedAt),
                nameof(SaleOverviewModel.LastModifiedFromPlatform)
            ]);
        }
        else
        {
            // Summary columns - key fields only (matching Excel export)
            columnOrder =
            [
                nameof(SaleOverviewModel.TransactionNo),
                nameof(SaleOverviewModel.OrderTransactionNo),
                nameof(SaleOverviewModel.TransactionDateTime),
                nameof(SaleOverviewModel.TotalQuantity),
                nameof(SaleOverviewModel.TotalAfterTax),
                nameof(SaleOverviewModel.DiscountPercent),
                nameof(SaleOverviewModel.DiscountAmount),
                nameof(SaleOverviewModel.TotalAmount),
                nameof(SaleOverviewModel.PaymentModes)
            ];

			// Add location column only if not showing location in header
			if (string.IsNullOrEmpty(locationName))
				columnOrder.Insert(3, nameof(SaleOverviewModel.LocationName));

            // Add party column only if not showing party in header
            if (string.IsNullOrEmpty(partyName))
            {
                int insertIndex = string.IsNullOrEmpty(locationName) ? 3 : 4;
                columnOrder.Insert(insertIndex, nameof(SaleOverviewModel.PartyName));
            }
        }

        // Customize specific columns for PDF display (matching Excel column names)
        columnSettings[nameof(SaleOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", IncludeInTotal = false };
        columnSettings[nameof(SaleOverviewModel.OrderTransactionNo)] = new() { DisplayName = "Order Trans No", IncludeInTotal = false };
        columnSettings[nameof(SaleOverviewModel.CompanyName)] = new() { DisplayName = "Company", IncludeInTotal = false };
        columnSettings[nameof(SaleOverviewModel.LocationName)] = new() { DisplayName = "Location", IncludeInTotal = false };
        columnSettings[nameof(SaleOverviewModel.PartyName)] = new() { DisplayName = "Party", IncludeInTotal = false };
        columnSettings[nameof(SaleOverviewModel.CustomerName)] = new() { DisplayName = "Customer", IncludeInTotal = false };
        columnSettings[nameof(SaleOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", IncludeInTotal = false };
        columnSettings[nameof(SaleOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", IncludeInTotal = false };
        columnSettings[nameof(SaleOverviewModel.Remarks)] = new() { DisplayName = "Remarks", IncludeInTotal = false };
        columnSettings[nameof(SaleOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", IncludeInTotal = false };
        columnSettings[nameof(SaleOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false };
        columnSettings[nameof(SaleOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", IncludeInTotal = false };
        columnSettings[nameof(SaleOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", IncludeInTotal = false };
        columnSettings[nameof(SaleOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false };
        columnSettings[nameof(SaleOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", IncludeInTotal = false };
        columnSettings[nameof(SaleOverviewModel.TotalItems)] = new()
        {
            DisplayName = "Items",
            Format = "#,##0",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(SaleOverviewModel.TotalQuantity)] = new()
        {
            DisplayName = "Qty",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(SaleOverviewModel.BaseTotal)] = new()
        {
            DisplayName = "Base Total",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(SaleOverviewModel.ItemDiscountAmount)] = new()
        {
            DisplayName = "Item Discount Amount",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(SaleOverviewModel.TotalAfterItemDiscount)] = new()
        {
            DisplayName = "After Disc",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(SaleOverviewModel.TotalInclusiveTaxAmount)] = new()
        {
            DisplayName = "Incl Tax",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(SaleOverviewModel.TotalExtraTaxAmount)] = new()
        {
            DisplayName = "Extra Tax",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(SaleOverviewModel.TotalAfterTax)] = new()
        {
            DisplayName = "Sub Total",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(SaleOverviewModel.OtherChargesPercent)] = new()
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

        columnSettings[nameof(SaleOverviewModel.OtherChargesAmount)] = new()
        {
            DisplayName = "Other Charges",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(SaleOverviewModel.DiscountPercent)] = new()
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

        columnSettings[nameof(SaleOverviewModel.DiscountAmount)] = new()
        {
            DisplayName = "Disc Amt",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(SaleOverviewModel.RoundOffAmount)] = new()
        {
            DisplayName = "Round Off",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(SaleOverviewModel.TotalAmount)] = new()
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

        columnSettings[nameof(SaleOverviewModel.Cash)] = new()
        {
            DisplayName = "Cash",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(SaleOverviewModel.Card)] = new()
        {
            DisplayName = "Card",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(SaleOverviewModel.UPI)] = new()
        {
            DisplayName = "UPI",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(SaleOverviewModel.Credit)] = new()
        {
            DisplayName = "Credit",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(SaleOverviewModel.PaymentModes)] = new()
        {
            DisplayName = "Payment Modes",
            IncludeInTotal = false
        };

        // Call the generic PDF export utility with landscape mode for all columns
        return await PDFReportExportUtil.ExportToPdf(
            saleData,
            "SALE REPORT",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            useLandscape: showAllColumns || showSummary,  // Use landscape when showing all columns
            locationName: locationName,
            partyName: partyName
        );
    }
}
