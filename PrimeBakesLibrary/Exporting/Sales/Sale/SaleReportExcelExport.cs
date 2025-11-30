using PrimeBakesLibrary.Models.Sales.Sale;

namespace PrimeBakesLibrary.Exporting.Sales.Sale;

/// <summary>
/// Excel export functionality for Sale Report
/// </summary>
public static class SaleReportExcelExport
{
    /// <summary>
    /// Export Sale Report to Excel with custom column order and formatting
    /// </summary>
    /// <param name="saleData">Collection of sale overview records</param>
    /// <param name="dateRangeStart">Start date of the report</param>
    /// <param name="dateRangeEnd">End date of the report</param>
    /// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
    /// <param name="showLocation">Whether to include location column</param>
    /// <param name="locationName">Name of the location for report header</param>
    /// <returns>MemoryStream containing the Excel file</returns>
    public static MemoryStream ExportSaleReport(
        IEnumerable<SaleOverviewModel> saleData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        bool showLocation = false,
        string locationName = null,
        string partyName = null)
    {
        // Define custom column settings
        var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
        {
            // IDs - Center aligned, no totals
            [nameof(SaleOverviewModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleOverviewModel.CompanyId)] = new() { DisplayName = "Company ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleOverviewModel.LocationId)] = new() { DisplayName = "Location ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleOverviewModel.PartyId)] = new() { DisplayName = "Party ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleOverviewModel.CustomerId)] = new() { DisplayName = "Customer ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleOverviewModel.OrderId)] = new() { DisplayName = "Order ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleOverviewModel.FinancialYearId)] = new() { DisplayName = "Financial Year ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleOverviewModel.CreatedBy)] = new() { DisplayName = "Created By ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleOverviewModel.LastModifiedBy)] = new() { DisplayName = "Modified By ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Text fields - Left aligned
            [nameof(SaleOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(SaleOverviewModel.OrderTransactionNo)] = new() { DisplayName = "Order Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(SaleOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(SaleOverviewModel.LocationName)] = new() { DisplayName = "Location", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(SaleOverviewModel.PartyName)] = new() { DisplayName = "Party", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(SaleOverviewModel.CustomerName)] = new() { DisplayName = "Customer", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(SaleOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(SaleOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(SaleOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(SaleOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(SaleOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(SaleOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

            // Dates - Center aligned with custom format
            [nameof(SaleOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            [nameof(SaleOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            [nameof(SaleOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },

            // Numeric fields - Right aligned with totals
            [nameof(SaleOverviewModel.TotalItems)] = new() { DisplayName = "Items", Format = "#,##0", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleOverviewModel.TotalQuantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },

            // Amount fields - All with N2 format and totals
            [nameof(SaleOverviewModel.BaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleOverviewModel.ItemDiscountAmount)] = new() { DisplayName = "Item Disc Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleOverviewModel.TotalAfterItemDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleOverviewModel.TotalInclusiveTaxAmount)] = new() { DisplayName = "Incl Tax", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleOverviewModel.TotalExtraTaxAmount)] = new() { DisplayName = "Extra Tax", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleOverviewModel.TotalAfterTax)] = new() { DisplayName = "Sub Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleOverviewModel.OtherChargesAmount)] = new() { DisplayName = "Other Charges", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleOverviewModel.DiscountAmount)] = new() { DisplayName = "Disc Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleOverviewModel.RoundOffAmount)] = new() { DisplayName = "Round Off", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleOverviewModel.TotalAmount)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, IsRequired = true, IsGrandTotal = true },
            [nameof(SaleOverviewModel.Cash)] = new() { DisplayName = "Cash", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleOverviewModel.Card)] = new() { DisplayName = "Card", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleOverviewModel.UPI)] = new() { DisplayName = "UPI", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleOverviewModel.Credit)] = new() { DisplayName = "Credit", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleOverviewModel.PaymentModes)] = new() { DisplayName = "Payment Modes", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },

            // Percentage fields - Center aligned, no totals
            [nameof(SaleOverviewModel.OtherChargesPercent)] = new() { DisplayName = "Other Charges %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleOverviewModel.DiscountPercent)] = new() { DisplayName = "Disc %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
        };

        // Define column order based on visibility setting
        List<string> columnOrder;

        if (showAllColumns)
        {
            // All columns - detailed view
            columnOrder =
            [
                nameof(SaleOverviewModel.TransactionNo),
                nameof(SaleOverviewModel.OrderTransactionNo),
                nameof(SaleOverviewModel.CompanyName)
            ];

            // Add location columns if showLocation is true
            if (showLocation)
                columnOrder.Add(nameof(SaleOverviewModel.LocationName));

            // Continue with remaining columns
            columnOrder.AddRange(
            [
                nameof(SaleOverviewModel.PartyName),
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
            // Summary columns - key fields only
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
            if (!showLocation)
                columnOrder.Insert(3, nameof(SaleOverviewModel.LocationName));

            // Add party column only if not showing party in header
            if (string.IsNullOrEmpty(partyName))
            {
                int insertIndex = showLocation ? 3 : 4;
                columnOrder.Insert(insertIndex, nameof(SaleOverviewModel.PartyName));
            }
        }

        // Call the generic Excel export utility
        return ExcelExportUtil.ExportToExcel(
            saleData,
            "SALE REPORT",
            "Sale Transactions",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            locationName: locationName,
            partyName: partyName
        );
    }
}
