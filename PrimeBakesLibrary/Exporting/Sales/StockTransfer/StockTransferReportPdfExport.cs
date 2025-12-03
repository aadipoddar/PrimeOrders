using PrimeBakesLibrary.Models.Sales.StockTransfer;

namespace PrimeBakesLibrary.Exporting.Sales.StockTransfer;

public static class StockTransferReportPdfExport
{
    public static async Task<MemoryStream> ExportStockTransferReport(
        IEnumerable<StockTransferOverviewModel> data,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        bool showLocation = false,
        string locationName = null,
        bool showToLocation = false,
        string toLocationName = null)
    {
        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>();

        List<string> columnOrder;

        bool showToLocationColumn = string.IsNullOrEmpty(toLocationName);

        if (showAllColumns)
        {
            columnOrder =
            [
                nameof(StockTransferOverviewModel.TransactionNo),
                nameof(StockTransferOverviewModel.CompanyName)
            ];

            if (showLocation)
                columnOrder.Add(nameof(StockTransferOverviewModel.LocationName));
            if (showToLocationColumn)
                columnOrder.Add(nameof(StockTransferOverviewModel.ToLocationName));

            columnOrder.AddRange(
            [
                nameof(StockTransferOverviewModel.TransactionDateTime),
                nameof(StockTransferOverviewModel.FinancialYear),
                nameof(StockTransferOverviewModel.TotalItems),
                nameof(StockTransferOverviewModel.TotalQuantity),
                nameof(StockTransferOverviewModel.BaseTotal),
                nameof(StockTransferOverviewModel.ItemDiscountAmount),
                nameof(StockTransferOverviewModel.TotalAfterItemDiscount),
                nameof(StockTransferOverviewModel.TotalInclusiveTaxAmount),
                nameof(StockTransferOverviewModel.TotalExtraTaxAmount),
                nameof(StockTransferOverviewModel.TotalAfterTax),
                nameof(StockTransferOverviewModel.OtherChargesPercent),
                nameof(StockTransferOverviewModel.OtherChargesAmount),
                nameof(StockTransferOverviewModel.DiscountPercent),
                nameof(StockTransferOverviewModel.DiscountAmount),
                nameof(StockTransferOverviewModel.RoundOffAmount),
                nameof(StockTransferOverviewModel.TotalAmount),
                nameof(StockTransferOverviewModel.Cash),
                nameof(StockTransferOverviewModel.Card),
                nameof(StockTransferOverviewModel.UPI),
                nameof(StockTransferOverviewModel.Credit),
                nameof(StockTransferOverviewModel.PaymentModes),
                nameof(StockTransferOverviewModel.Remarks),
                nameof(StockTransferOverviewModel.CreatedByName),
                nameof(StockTransferOverviewModel.CreatedAt),
                nameof(StockTransferOverviewModel.CreatedFromPlatform),
                nameof(StockTransferOverviewModel.LastModifiedByUserName),
                nameof(StockTransferOverviewModel.LastModifiedAt),
                nameof(StockTransferOverviewModel.LastModifiedFromPlatform)
            ]);
        }
        else
        {
            columnOrder =
            [
                nameof(StockTransferOverviewModel.TransactionNo),
                nameof(StockTransferOverviewModel.TransactionDateTime),
                nameof(StockTransferOverviewModel.TotalQuantity),
                nameof(StockTransferOverviewModel.TotalAfterTax),
                nameof(StockTransferOverviewModel.DiscountPercent),
                nameof(StockTransferOverviewModel.DiscountAmount),
                nameof(StockTransferOverviewModel.TotalAmount),
                nameof(StockTransferOverviewModel.PaymentModes)
            ];

            if (!showLocation)
                columnOrder.Insert(2, nameof(StockTransferOverviewModel.LocationName));
            if (!showToLocation)
                columnOrder.Insert(3, nameof(StockTransferOverviewModel.ToLocationName));
        }

        // Map display names and formats similar to Excel export
        columnSettings[nameof(StockTransferOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", IncludeInTotal = false };
        columnSettings[nameof(StockTransferOverviewModel.CompanyName)] = new() { DisplayName = "Company", IncludeInTotal = false };
        columnSettings[nameof(StockTransferOverviewModel.LocationName)] = new() { DisplayName = "From Location", IncludeInTotal = false };
        columnSettings[nameof(StockTransferOverviewModel.ToLocationName)] = new() { DisplayName = "To Location", IncludeInTotal = false };
        columnSettings[nameof(StockTransferOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", IncludeInTotal = false };
        columnSettings[nameof(StockTransferOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", IncludeInTotal = false };
        columnSettings[nameof(StockTransferOverviewModel.Remarks)] = new() { DisplayName = "Remarks", IncludeInTotal = false };
        columnSettings[nameof(StockTransferOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", IncludeInTotal = false };
        columnSettings[nameof(StockTransferOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false };
        columnSettings[nameof(StockTransferOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", IncludeInTotal = false };
        columnSettings[nameof(StockTransferOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", IncludeInTotal = false };
        columnSettings[nameof(StockTransferOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false };
        columnSettings[nameof(StockTransferOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", IncludeInTotal = false };

        columnSettings[nameof(StockTransferOverviewModel.TotalItems)] = new() { DisplayName = "Items", Format = "#,##0", StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat { Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle } };
        columnSettings[nameof(StockTransferOverviewModel.TotalQuantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat { Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle } };
        columnSettings[nameof(StockTransferOverviewModel.BaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat { Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle } };
        columnSettings[nameof(StockTransferOverviewModel.ItemDiscountAmount)] = new() { DisplayName = "Item Disc Amt", Format = "#,##0.00", StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat { Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle } };
        columnSettings[nameof(StockTransferOverviewModel.TotalAfterItemDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat { Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle } };
        columnSettings[nameof(StockTransferOverviewModel.TotalInclusiveTaxAmount)] = new() { DisplayName = "Incl Tax", Format = "#,##0.00", StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat { Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle } };
        columnSettings[nameof(StockTransferOverviewModel.TotalExtraTaxAmount)] = new() { DisplayName = "Extra Tax", Format = "#,##0.00", StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat { Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle } };
        columnSettings[nameof(StockTransferOverviewModel.TotalAfterTax)] = new() { DisplayName = "Sub Total", Format = "#,##0.00", StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat { Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle } };
        columnSettings[nameof(StockTransferOverviewModel.OtherChargesPercent)] = new() { DisplayName = "Other Charges %", Format = "#,##0.00", IncludeInTotal = false, StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat { Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center, LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle } };
        columnSettings[nameof(StockTransferOverviewModel.OtherChargesAmount)] = new() { DisplayName = "Other Charges", Format = "#,##0.00", StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat { Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle } };
        columnSettings[nameof(StockTransferOverviewModel.DiscountPercent)] = new() { DisplayName = "Disc %", Format = "#,##0.00", IncludeInTotal = false, StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat { Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center, LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle } };
        columnSettings[nameof(StockTransferOverviewModel.DiscountAmount)] = new() { DisplayName = "Disc Amt", Format = "#,##0.00", StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat { Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle } };
        columnSettings[nameof(StockTransferOverviewModel.RoundOffAmount)] = new() { DisplayName = "Round Off", Format = "#,##0.00", StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat { Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle } };
        columnSettings[nameof(StockTransferOverviewModel.TotalAmount)] = new() { DisplayName = "Total", Format = "#,##0.00", IsRequired = true, IsGrandTotal = true, StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat { Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle } };
        columnSettings[nameof(StockTransferOverviewModel.Cash)] = new() { DisplayName = "Cash", Format = "#,##0.00", StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat { Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle } };
        columnSettings[nameof(StockTransferOverviewModel.Card)] = new() { DisplayName = "Card", Format = "#,##0.00", StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat { Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle } };
        columnSettings[nameof(StockTransferOverviewModel.UPI)] = new() { DisplayName = "UPI", Format = "#,##0.00", StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat { Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle } };
        columnSettings[nameof(StockTransferOverviewModel.Credit)] = new() { DisplayName = "Credit", Format = "#,##0.00", StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat { Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle } };
        columnSettings[nameof(StockTransferOverviewModel.PaymentModes)] = new() { DisplayName = "Payment Modes", IncludeInTotal = false };

        return await PDFReportExportUtil.ExportToPdf(
            data,
            "STOCK TRANSFER REPORT",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            useLandscape: showAllColumns,
            locationName: locationName,
            partyName: toLocationName
        );
    }
}
