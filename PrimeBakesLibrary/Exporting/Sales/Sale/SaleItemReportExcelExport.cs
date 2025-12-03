using PrimeBakesLibrary.Models.Sales.Sale;

namespace PrimeBakesLibrary.Exporting.Sales.Sale;

/// <summary>
/// Excel export functionality for Sale Item Report
/// </summary>
public static class SaleItemReportExcelExport
{
    /// <summary>
    /// Export Sale Item Report to Excel with custom column order and formatting
    /// </summary>
    /// <param name="saleItemData">Collection of sale item overview records</param>
    /// <param name="dateRangeStart">Start date of the report</param>
    /// <param name="dateRangeEnd">End date of the report</param>
    /// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
    /// <param name="showLocation">Whether to include location column (for location ID 1 users)</param>
    /// <returns>MemoryStream containing the Excel file</returns>
    public static async Task<MemoryStream> ExportSaleItemReport(
        IEnumerable<SaleItemOverviewModel> saleItemData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        bool showLocation = false,
        string locationName = null)
    {
        // Define custom column settings
        var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
        {
            // IDs - Center aligned, no totals
            [nameof(SaleItemOverviewModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleItemOverviewModel.MasterId)] = new() { DisplayName = "Master ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleItemOverviewModel.ItemCategoryId)] = new() { DisplayName = "Category ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleItemOverviewModel.CompanyId)] = new() { DisplayName = "Company ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleItemOverviewModel.PartyId)] = new() { DisplayName = "Party ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            
            // Text fields
            [nameof(SaleItemOverviewModel.ItemName)] = new() { DisplayName = "Item", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(SaleItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(SaleItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(SaleItemOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(SaleItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(SaleItemOverviewModel.LocationName)] = new() { DisplayName = "Location", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(SaleItemOverviewModel.PartyName)] = new() { DisplayName = "Party", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(SaleItemOverviewModel.SaleRemarks)] = new() { DisplayName = "Sale Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(SaleItemOverviewModel.Remarks)] = new() { DisplayName = "Item Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

            // Date fields
            [nameof(SaleItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },

            // Numeric fields - Quantity
            [nameof(SaleItemOverviewModel.Quantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleItemOverviewModel.Rate)] = new() { DisplayName = "Rate", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false },
            [nameof(SaleItemOverviewModel.NetRate)] = new() { DisplayName = "Net Rate", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false },

            // Amount fields - All with N2 format and totals
            [nameof(SaleItemOverviewModel.BaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleItemOverviewModel.DiscountAmount)] = new() { DisplayName = "Discount Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleItemOverviewModel.AfterDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleItemOverviewModel.SGSTAmount)] = new() { DisplayName = "SGST Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleItemOverviewModel.CGSTAmount)] = new() { DisplayName = "CGST Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleItemOverviewModel.IGSTAmount)] = new() { DisplayName = "IGST Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleItemOverviewModel.TotalTaxAmount)] = new() { DisplayName = "Tax", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleItemOverviewModel.Total)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleItemOverviewModel.NetTotal)] = new() { DisplayName = "Net Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },

            // Percentage fields - Center aligned
            [nameof(SaleItemOverviewModel.DiscountPercent)] = new() { DisplayName = "Disc %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleItemOverviewModel.SGSTPercent)] = new() { DisplayName = "SGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleItemOverviewModel.CGSTPercent)] = new() { DisplayName = "CGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleItemOverviewModel.IGSTPercent)] = new() { DisplayName = "IGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Boolean fields
            [nameof(SaleItemOverviewModel.InclusiveTax)] = new() { DisplayName = "Incl Tax", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
        };

        // Define column order based on showAllColumns flag
        List<string> columnOrder;

        // All columns in logical order
        if (showAllColumns)
        {
            List<string> columns =
            [
                nameof(SaleItemOverviewModel.ItemName),
                nameof(SaleItemOverviewModel.ItemCode),
                nameof(SaleItemOverviewModel.ItemCategoryName),
                nameof(SaleItemOverviewModel.TransactionNo),
                nameof(SaleItemOverviewModel.TransactionDateTime),
                nameof(SaleItemOverviewModel.CompanyName)
            ];

            if (showLocation)
                columns.Add(nameof(SaleItemOverviewModel.LocationName));

            columns.AddRange([
                nameof(SaleItemOverviewModel.PartyName),
                nameof(SaleItemOverviewModel.Quantity),
                nameof(SaleItemOverviewModel.Rate),
                nameof(SaleItemOverviewModel.BaseTotal),
                nameof(SaleItemOverviewModel.DiscountPercent),
                nameof(SaleItemOverviewModel.DiscountAmount),
                nameof(SaleItemOverviewModel.AfterDiscount),
                nameof(SaleItemOverviewModel.SGSTPercent),
                nameof(SaleItemOverviewModel.SGSTAmount),
                nameof(SaleItemOverviewModel.CGSTPercent),
                nameof(SaleItemOverviewModel.CGSTAmount),
                nameof(SaleItemOverviewModel.IGSTPercent),
                nameof(SaleItemOverviewModel.IGSTAmount),
                nameof(SaleItemOverviewModel.TotalTaxAmount),
                nameof(SaleItemOverviewModel.InclusiveTax),
                nameof(SaleItemOverviewModel.Total),
                nameof(SaleItemOverviewModel.NetRate),
                nameof(SaleItemOverviewModel.NetTotal),
                nameof(SaleItemOverviewModel.SaleRemarks),
                nameof(SaleItemOverviewModel.Remarks)
            ]);

            columnOrder = columns;
        }
        // Summary columns only
        else
            columnOrder =
            [
                nameof(SaleItemOverviewModel.ItemName),
                nameof(SaleItemOverviewModel.ItemCode),
                nameof(SaleItemOverviewModel.TransactionNo),
                nameof(SaleItemOverviewModel.TransactionDateTime),
                nameof(SaleItemOverviewModel.LocationName),
                nameof(SaleItemOverviewModel.PartyName),
                nameof(SaleItemOverviewModel.Quantity),
                nameof(SaleItemOverviewModel.NetRate),
                nameof(SaleItemOverviewModel.NetTotal)
            ];

        // Export using the generic utility
        return await ExcelExportUtil.ExportToExcel(
            saleItemData,
            "SALE ITEM REPORT",
            "Sale Item Transactions",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            locationName
        );
    }
}
