using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Sales.Sale;
using PrimeBakesLibrary.Models.Sales.StockTransfer;

namespace PrimeBakesLibrary.Exporting.Sales.StockTransfer;

public static class StockTransferReportExcelExport
{
    public static async Task<MemoryStream> ExportStockTransferReport(
        IEnumerable<StockTransferOverviewModel> data,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        bool showLocation = false,
        string locationName = null,
        bool showToLocation = false,
        string toLocationName = null,
        bool showSummary = false)
    {
        var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
        {
            [nameof(StockTransferOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(StockTransferOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            [nameof(StockTransferOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(StockTransferOverviewModel.LocationName)] = new() { DisplayName = "From Location", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(StockTransferOverviewModel.ToLocationName)] = new() { DisplayName = "To Location", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(StockTransferOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(StockTransferOverviewModel.TotalItems)] = new() { DisplayName = "Items", Format = "#,##0", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(StockTransferOverviewModel.TotalQuantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(StockTransferOverviewModel.BaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(StockTransferOverviewModel.ItemDiscountAmount)] = new() { DisplayName = "Item Disc Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(StockTransferOverviewModel.TotalAfterItemDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(StockTransferOverviewModel.TotalInclusiveTaxAmount)] = new() { DisplayName = "Incl Tax", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(StockTransferOverviewModel.TotalExtraTaxAmount)] = new() { DisplayName = "Extra Tax", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(StockTransferOverviewModel.TotalAfterTax)] = new() { DisplayName = "Sub Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(StockTransferOverviewModel.OtherChargesPercent)] = new() { DisplayName = "Other Charges %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(StockTransferOverviewModel.OtherChargesAmount)] = new() { DisplayName = "Other Charges", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(StockTransferOverviewModel.DiscountPercent)] = new() { DisplayName = "Disc %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(StockTransferOverviewModel.DiscountAmount)] = new() { DisplayName = "Disc Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(StockTransferOverviewModel.RoundOffAmount)] = new() { DisplayName = "Round Off", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(StockTransferOverviewModel.TotalAmount)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, IsRequired = true, IsGrandTotal = true },
            [nameof(StockTransferOverviewModel.Cash)] = new() { DisplayName = "Cash", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(StockTransferOverviewModel.Card)] = new() { DisplayName = "Card", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(StockTransferOverviewModel.UPI)] = new() { DisplayName = "UPI", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(StockTransferOverviewModel.Credit)] = new() { DisplayName = "Credit", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(StockTransferOverviewModel.PaymentModes)] = new() { DisplayName = "Payment Modes", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            [nameof(StockTransferOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(StockTransferOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(StockTransferOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            [nameof(StockTransferOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(StockTransferOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(StockTransferOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            [nameof(StockTransferOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft }
        };

        List<string> columnOrder;

		// Summary view - grouped by party with totals
		if (showSummary)
			columnOrder =
			[
				nameof(StockTransferOverviewModel.ToLocationName),
				nameof(StockTransferOverviewModel.TotalItems),
				nameof(StockTransferOverviewModel.TotalQuantity),
				nameof(StockTransferOverviewModel.BaseTotal),
				nameof(StockTransferOverviewModel.ItemDiscountAmount),
				nameof(StockTransferOverviewModel.TotalAfterItemDiscount),
				nameof(StockTransferOverviewModel.TotalInclusiveTaxAmount),
				nameof(StockTransferOverviewModel.TotalExtraTaxAmount),
				nameof(StockTransferOverviewModel.TotalAfterTax),
				nameof(StockTransferOverviewModel.OtherChargesAmount),
				nameof(StockTransferOverviewModel.DiscountAmount),
				nameof(StockTransferOverviewModel.RoundOffAmount),
				nameof(StockTransferOverviewModel.TotalAmount),
				nameof(StockTransferOverviewModel.Cash),
				nameof(StockTransferOverviewModel.Card),
				nameof(StockTransferOverviewModel.UPI),
				nameof(StockTransferOverviewModel.Credit)
			];

		else if (showAllColumns)
        {
            columnOrder =
            [
                nameof(StockTransferOverviewModel.TransactionNo),
                nameof(StockTransferOverviewModel.CompanyName)
            ];

            if (showLocation)
                columnOrder.Add(nameof(StockTransferOverviewModel.LocationName));
            if (showToLocation)
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

        return await ExcelReportExportUtil.ExportToExcel(
            data,
            "STOCK TRANSFER REPORT",
            "Stock Transfers",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            locationName: locationName,
            partyName: toLocationName
        );
    }
}
