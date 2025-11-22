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
    /// <param name="showLocation">Whether to include location column</param>
    /// <param name="locationName">Name of the location for report header</param>
    /// <returns>MemoryStream containing the PDF file</returns>
    public static MemoryStream ExportSaleReport(
        IEnumerable<SaleOverviewModel> saleData,
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
                "OrderTransactionNo",
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
                "PartyName",
                "CustomerName",
                "TransactionDateTime",
                "FinancialYear",
                "TotalItems",
                "TotalQuantity",
                "BaseTotal",
                "ItemDiscountPercent",
                "ItemDiscountAmount",
                "AfterDiscount",
                "SGSTPercent",
                "CGSTPercent",
                "IGSTPercent",
                "SGSTAmount",
                "CGSTAmount",
                "IGSTAmount",
                "TotalTaxAmount",
                "TotalAfterTax",
                "OtherChargesPercent",
                "OtherChargesAmount",
                "TotalAfterOtherCharges",
                "DiscountPercent",
                "DiscountAmount",
                "TotalAfterDiscount",
                "RoundOffAmount",
                "TotalAmount",
                "Cash",
                "Card",
                "UPI",
                "Credit",
                "PaymentModes",
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
                "OrderTransactionNo",
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
                "PartyName",
                "CustomerName",
                "TotalQuantity",
                "TotalAfterTax",
                "DiscountPercent",
                "TotalAmount",
                "PaymentModes"
            ]);
        }

        // Customize specific columns for PDF display (matching Excel column names)
        columnSettings["TransactionNo"] = new() { DisplayName = "Transaction No", IncludeInTotal = false };
        columnSettings["OrderTransactionNo"] = new() { DisplayName = "Order Transaction No", IncludeInTotal = false };
        columnSettings["CompanyName"] = new() { DisplayName = "Company Name", IncludeInTotal = false };
        columnSettings["LocationName"] = new() { DisplayName = "Location Name", IncludeInTotal = false };
        columnSettings["PartyName"] = new() { DisplayName = "Party Name", IncludeInTotal = false };
        columnSettings["CustomerName"] = new() { DisplayName = "Customer Name", IncludeInTotal = false };
        columnSettings["TransactionDateTime"] = new() { DisplayName = "Transaction Date", Format = "dd-MMM-yyyy hh:mm tt", IncludeInTotal = false };
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

        columnSettings["BaseTotal"] = new()
        {
            DisplayName = "Base Total",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings["ItemDiscountPercent"] = new()
        {
            DisplayName = "Item Discount %",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            },
            IncludeInTotal = false
        };

        columnSettings["ItemDiscountAmount"] = new()
        {
            DisplayName = "Item Discount Amount",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings["AfterDiscount"] = new()
        {
            DisplayName = "After Discount",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings["SGSTPercent"] = new()
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

        columnSettings["CGSTPercent"] = new()
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

        columnSettings["IGSTPercent"] = new()
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

        columnSettings["SGSTAmount"] = new()
        {
            DisplayName = "SGST Amount",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings["CGSTAmount"] = new()
        {
            DisplayName = "CGST Amount",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings["IGSTAmount"] = new()
        {
            DisplayName = "IGST Amount",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings["TotalTaxAmount"] = new()
        {
            DisplayName = "Total Tax",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings["TotalAfterTax"] = new()
        {
            DisplayName = "Sub Total",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings["OtherChargesPercent"] = new()
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

        columnSettings["OtherChargesAmount"] = new()
        {
            DisplayName = "Other Charges",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings["TotalAfterOtherCharges"] = new()
        {
            DisplayName = "After Other Charges",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings["DiscountPercent"] = new()
        {
            DisplayName = "Discount %",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            },
            IncludeInTotal = false
        };

        columnSettings["DiscountAmount"] = new()
        {
            DisplayName = "Discount Amount",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings["TotalAfterDiscount"] = new()
        {
            DisplayName = "After Discount",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings["RoundOffAmount"] = new()
        {
            DisplayName = "Round Off",
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
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings["Cash"] = new()
        {
            DisplayName = "Cash",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings["Card"] = new()
        {
            DisplayName = "Card",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings["UPI"] = new()
        {
            DisplayName = "UPI",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings["Credit"] = new()
        {
            DisplayName = "Credit",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings["PaymentModes"] = new()
        {
            DisplayName = "Payment Modes",
            IncludeInTotal = false
        };

        // Call the generic PDF export utility with landscape mode for all columns
        return PDFReportExportUtil.ExportToPdf(
            saleData,
            "SALE REPORT",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            useLandscape: showAllColumns,  // Use landscape when showing all columns
            locationName: locationName
        );
    }
}
