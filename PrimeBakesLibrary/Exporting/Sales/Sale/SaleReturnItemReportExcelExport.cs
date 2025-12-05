using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Sales.Sale;

namespace PrimeBakesLibrary.Exporting.Sales.Sale;

/// <summary>
/// Excel export functionality for Sale Return Item Report
/// </summary>
public static class SaleReturnItemReportExcelExport
{
    /// <summary>
    /// Export Sale Return Item Report to Excel with custom column order and formatting
    /// </summary>
    /// <param name="saleReturnItemData">Collection of sale return item overview records</param>
    /// <param name="dateRangeStart">Start date of the report</param>
    /// <param name="dateRangeEnd">End date of the report</param>
    /// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
    /// <param name="showLocation">Whether to include location column (for location ID 1 users)</param>
    /// <returns>MemoryStream containing the Excel file</returns>
    public static async Task<MemoryStream> ExportSaleReturnItemReport(
        IEnumerable<SaleReturnItemOverviewModel> saleReturnItemData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        bool showLocation = false,
        string locationName = null)
    {
        // Define custom column settings
        var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
        {
            // IDs - Center aligned, no totals
            [nameof(SaleReturnItemOverviewModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleReturnItemOverviewModel.MasterId)] = new() { DisplayName = "Master ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleReturnItemOverviewModel.ItemCategoryId)] = new() { DisplayName = "Category ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleReturnItemOverviewModel.CompanyId)] = new() { DisplayName = "Company ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleReturnItemOverviewModel.PartyId)] = new() { DisplayName = "Party ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Text fields
            [nameof(SaleReturnItemOverviewModel.ItemName)] = new() { DisplayName = "Item", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(SaleReturnItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(SaleReturnItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(SaleReturnItemOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(SaleReturnItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(SaleReturnItemOverviewModel.LocationName)] = new() { DisplayName = "Location", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(SaleReturnItemOverviewModel.PartyName)] = new() { DisplayName = "Party", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(SaleReturnItemOverviewModel.SaleReturnRemarks)] = new() { DisplayName = "Sale Return Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(SaleReturnItemOverviewModel.Remarks)] = new() { DisplayName = "Item Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

            // Date fields
            [nameof(SaleReturnItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            
            // Numeric fields - Quantity
            [nameof(SaleReturnItemOverviewModel.Quantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleReturnItemOverviewModel.Rate)] = new() { DisplayName = "Rate", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false },
            [nameof(SaleReturnItemOverviewModel.NetRate)] = new() { DisplayName = "Net Rate", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false },
            
            // Amount fields - All with N2 format and totals
            [nameof(SaleReturnItemOverviewModel.BaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleReturnItemOverviewModel.DiscountAmount)] = new() { DisplayName = "Discount Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleReturnItemOverviewModel.AfterDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleReturnItemOverviewModel.SGSTAmount)] = new() { DisplayName = "SGST Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleReturnItemOverviewModel.CGSTAmount)] = new() { DisplayName = "CGST Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleReturnItemOverviewModel.IGSTAmount)] = new() { DisplayName = "IGST Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleReturnItemOverviewModel.TotalTaxAmount)] = new() { DisplayName = "Tax", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleReturnItemOverviewModel.Total)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(SaleReturnItemOverviewModel.NetTotal)] = new() { DisplayName = "Net Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            
            // Percentage fields - Center aligned
            [nameof(SaleReturnItemOverviewModel.DiscountPercent)] = new() { DisplayName = "Disc %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleReturnItemOverviewModel.SGSTPercent)] = new() { DisplayName = "SGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleReturnItemOverviewModel.CGSTPercent)] = new() { DisplayName = "CGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(SaleReturnItemOverviewModel.IGSTPercent)] = new() { DisplayName = "IGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Boolean fields
            [nameof(SaleReturnItemOverviewModel.InclusiveTax)] = new() { DisplayName = "Incl Tax", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
        };

        // Define column order based on showAllColumns flag
        List<string> columnOrder;

        // All columns in logical order
        if (showAllColumns)
        {
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
        // Summary columns only
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

        // Export using the generic utility
        return await ExcelReportExportUtil.ExportToExcel(
            saleReturnItemData,
            "SALE RETURN ITEM REPORT",
            "Sale Return Item Transactions",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            locationName
        );
    }
}
