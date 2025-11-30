using PrimeBakesLibrary.Models.Sales.Sale;

namespace PrimeBakesLibrary.Exporting.Sales.Sale;

/// <summary>
/// Excel export functionality for Sale Return Report
/// </summary>
public static class SaleReturnReportExcelExport
{
    /// <summary>
    /// Export Sale Return Report to Excel with custom column order and formatting
    /// </summary>
    /// <param name="saleReturnData">Collection of sale return overview records</param>
    /// <param name="dateRangeStart">Start date of the report</param>
    /// <param name="dateRangeEnd">End date of the report</param>
    /// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
    /// <param name="showLocation">Whether to include location column</param>
    /// <param name="locationName">Name of the location for report header</param>
    /// <returns>MemoryStream containing the Excel file</returns>
    public static MemoryStream ExportSaleReturnReport(
        IEnumerable<SaleReturnOverviewModel> saleReturnData,
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
            [nameof(SaleReturnOverviewModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleReturnOverviewModel.CompanyId)] = new() { DisplayName = "Company ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleReturnOverviewModel.LocationId)] = new() { DisplayName = "Location ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleReturnOverviewModel.PartyId)] = new() { DisplayName = "Party ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleReturnOverviewModel.CustomerId)] = new() { DisplayName = "Customer ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleReturnOverviewModel.FinancialYearId)] = new() { DisplayName = "Financial Year ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleReturnOverviewModel.CreatedBy)] = new() { DisplayName = "Created By ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleReturnOverviewModel.LastModifiedBy)] = new() { DisplayName = "Modified By ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Text fields - Left aligned
            [nameof(SaleReturnOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(SaleReturnOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(SaleReturnOverviewModel.LocationName)] = new() { DisplayName = "Location", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(SaleReturnOverviewModel.PartyName)] = new() { DisplayName = "Party", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(SaleReturnOverviewModel.CustomerName)] = new() { DisplayName = "Customer", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(SaleReturnOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(SaleReturnOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(SaleReturnOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(SaleReturnOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(SaleReturnOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(SaleReturnOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

            // Dates - Center aligned with custom format
            [nameof(SaleReturnOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            [nameof(SaleReturnOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            [nameof(SaleReturnOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },

            // Numeric fields - Right aligned with totals
            [nameof(SaleReturnOverviewModel.TotalItems)] = new() { DisplayName = "Items", Format = "#,##0", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleReturnOverviewModel.TotalQuantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },

            // Amount fields - All with N2 format and totals
            [nameof(SaleReturnOverviewModel.BaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleReturnOverviewModel.ItemDiscountAmount)] = new() { DisplayName = "Item Disc Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleReturnOverviewModel.TotalAfterItemDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleReturnOverviewModel.TotalInclusiveTaxAmount)] = new() { DisplayName = "Incl Tax", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleReturnOverviewModel.TotalExtraTaxAmount)] = new() { DisplayName = "Extra Tax", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleReturnOverviewModel.TotalAfterTax)] = new() { DisplayName = "Sub Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleReturnOverviewModel.OtherChargesAmount)] = new() { DisplayName = "Other Charges", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleReturnOverviewModel.DiscountAmount)] = new() { DisplayName = "Disc Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleReturnOverviewModel.RoundOffAmount)] = new() { DisplayName = "Round Off", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleReturnOverviewModel.TotalAmount)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, IsRequired = true, IsGrandTotal = true },
            [nameof(SaleReturnOverviewModel.Cash)] = new() { DisplayName = "Cash", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleReturnOverviewModel.Card)] = new() { DisplayName = "Card", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleReturnOverviewModel.UPI)] = new() { DisplayName = "UPI", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleReturnOverviewModel.Credit)] = new() { DisplayName = "Credit", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(SaleReturnOverviewModel.PaymentModes)] = new() { DisplayName = "Payment Modes", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },

            // Percentage fields - Center aligned, no totals
            [nameof(SaleReturnOverviewModel.OtherChargesPercent)] = new() { DisplayName = "Other Charges %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleReturnOverviewModel.DiscountPercent)] = new() { DisplayName = "Disc %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
        };

        // Define column order based on visibility setting
        List<string> columnOrder;

        if (showAllColumns)
        {
            // All columns - detailed view
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
            // Summary columns - key fields only
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

        // Call the generic Excel export utility
        return ExcelExportUtil.ExportToExcel(
            saleReturnData,
            "SALE RETURN REPORT",
            "Sale Return Transactions",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            locationName: locationName
        );
    }
}
