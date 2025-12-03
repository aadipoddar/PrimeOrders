using PrimeBakesLibrary.Models.Inventory.Stock;

namespace PrimeBakesLibrary.Exporting.Inventory.Stock;

/// <summary>
/// Excel export functionality for Raw Material Stock Report
/// </summary>
public static class RawMaterialStockReportExcelExport
{
    /// <summary>
    /// Export Raw Material Stock Report to Excel with custom column order and formatting
    /// </summary>
    /// <param name="stockData">Collection of raw material stock summary records</param>
    /// <param name="dateRangeStart">Start date of the report</param>
    /// <param name="dateRangeEnd">End date of the report</param>
    /// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
    /// <param name="stockDetailsData">Optional collection of raw material stock detail records for second worksheet</param>
    /// <returns>MemoryStream containing the Excel file</returns>
    public static async Task<MemoryStream> ExportRawMaterialStockReport(
        IEnumerable<RawMaterialStockSummaryModel> stockData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        IEnumerable<RawMaterialStockDetailsModel> stockDetailsData = null)
    {
        // Define custom column settings
        var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
        {
            // IDs - Center aligned, no totals
            [ nameof(RawMaterialStockSummaryModel.RawMaterialId)] = new() { DisplayName = "Material ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false, Width = 12 },
            [nameof(RawMaterialStockSummaryModel.RawMaterialCategoryId)] = new() { DisplayName = "Category ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false, Width = 12 },

            // Text fields
            [nameof(RawMaterialStockSummaryModel.RawMaterialName)] = new() { DisplayName = "Raw Material", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, Width = 25 },
            [nameof(RawMaterialStockSummaryModel.RawMaterialCode)] = new() { DisplayName = "Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, Width = 15 },
            [nameof(RawMaterialStockSummaryModel.RawMaterialCategoryName)] = new() { DisplayName = "Category", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, Width = 20 },
            [nameof(RawMaterialStockSummaryModel.UnitOfMeasurement)] = new() { DisplayName = "UOM", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, Width = 10 },

            // Stock quantity fields - All with totals
            [nameof(RawMaterialStockSummaryModel.OpeningStock)] = new() { DisplayName = "Opening Stock", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 15 },
            [nameof(RawMaterialStockSummaryModel.PurchaseStock)] = new() { DisplayName = "Purchase Stock", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 15 },
            [nameof(RawMaterialStockSummaryModel.SaleStock)] = new() { DisplayName = "Sale Stock", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 15 },
            [nameof(RawMaterialStockSummaryModel.MonthlyStock)] = new() { DisplayName = "Monthly Stock", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 15 },
            [nameof(RawMaterialStockSummaryModel.ClosingStock)] = new() { DisplayName = "Closing Stock", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 15 },

            [nameof(RawMaterialStockSummaryModel.Rate)] = new() { DisplayName = "Rate", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false, Width = 12 },
            [nameof(RawMaterialStockSummaryModel.ClosingValue)] = new() { DisplayName = "Closing Value", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 15 },

            [nameof(RawMaterialStockSummaryModel.AveragePrice)] = new() { DisplayName = "Average Price", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false, Width = 15 },
            [nameof(RawMaterialStockSummaryModel.WeightedAverageValue)] = new() { DisplayName = "Weighted Avg Value", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 18 },

            [nameof(RawMaterialStockSummaryModel.LastPurchasePrice)] = new() { DisplayName = "Last Purchase Price", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false, Width = 18 },
            [nameof(RawMaterialStockSummaryModel.LastPurchaseValue)] = new() { DisplayName = "Last Purchase Value", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 18 }
        };

        // Define column order based on showAllColumns flag
        List<string> columnOrder;

        // All columns in logical order
        if (showAllColumns)
            columnOrder =
            [
                nameof(RawMaterialStockSummaryModel.RawMaterialName),
                nameof(RawMaterialStockSummaryModel.RawMaterialCode),
                nameof(RawMaterialStockSummaryModel.RawMaterialCategoryName),
                nameof(RawMaterialStockSummaryModel.UnitOfMeasurement),
                nameof(RawMaterialStockSummaryModel.OpeningStock),
                nameof(RawMaterialStockSummaryModel.PurchaseStock),
                nameof(RawMaterialStockSummaryModel.SaleStock),
                nameof(RawMaterialStockSummaryModel.MonthlyStock),
                nameof(RawMaterialStockSummaryModel.ClosingStock),
                nameof(RawMaterialStockSummaryModel.Rate),
                nameof(RawMaterialStockSummaryModel.ClosingValue),
                nameof(RawMaterialStockSummaryModel.AveragePrice),
                nameof(RawMaterialStockSummaryModel.WeightedAverageValue),
                nameof(RawMaterialStockSummaryModel.LastPurchasePrice),
                nameof(RawMaterialStockSummaryModel.LastPurchaseValue)
            ];

        // Summary columns only (key information)
        else
            columnOrder =
            [
                nameof(RawMaterialStockSummaryModel.RawMaterialName),
                nameof(RawMaterialStockSummaryModel.UnitOfMeasurement),
                nameof(RawMaterialStockSummaryModel.OpeningStock),
                nameof(RawMaterialStockSummaryModel.PurchaseStock),
                nameof(RawMaterialStockSummaryModel.SaleStock),
                nameof(RawMaterialStockSummaryModel.ClosingStock),
                nameof(RawMaterialStockSummaryModel.Rate),
                nameof(RawMaterialStockSummaryModel.ClosingValue)
            ];

        // If no details data provided, use the simple single-worksheet export
        if (stockDetailsData == null || !stockDetailsData.Any())
            return await ExcelExportUtil.ExportToExcel(
                stockData,
                "RAW MATERIAL STOCK REPORT",
                "Stock Summary",
                dateRangeStart,
                dateRangeEnd,
                columnSettings,
                columnOrder
            );

        // Multi-worksheet export
        return await ExportWithDetails(
            stockData,
            stockDetailsData,
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder
        );
    }

    /// <summary>
    /// Export with both summary and details worksheets
    /// </summary>
    private static async Task<MemoryStream> ExportWithDetails(
        IEnumerable<RawMaterialStockSummaryModel> stockData,
        IEnumerable<RawMaterialStockDetailsModel> stockDetailsData,
        DateOnly? dateRangeStart,
        DateOnly? dateRangeEnd,
        Dictionary<string, ExcelExportUtil.ColumnSetting> summaryColumnSettings,
        List<string> summaryColumnOrder)
    {
        // Create the first worksheet with summary data
        var summaryStream = await ExcelExportUtil.ExportToExcel(
            stockData,
            "RAW MATERIAL STOCK REPORT",
            "Stock Summary",
            dateRangeStart,
            dateRangeEnd,
            summaryColumnSettings,
            summaryColumnOrder
        );

        // Define column settings for details worksheet
        var detailsColumnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
        {
            // IDs - Center aligned, no totals
            [nameof(RawMaterialStockDetailsModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false, Width = 10 },
            [nameof(RawMaterialStockDetailsModel.RawMaterialId)] = new() { DisplayName = "Material ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false, Width = 12 },
            [nameof(RawMaterialStockDetailsModel.TransactionId)] = new() { DisplayName = "Trans ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false, Width = 15 },

            // Text fields
            [nameof(RawMaterialStockDetailsModel.RawMaterialName)] = new() { DisplayName = "Raw Material", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, Width = 25 },
            [nameof(RawMaterialStockDetailsModel.RawMaterialCode)] = new() { DisplayName = "Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, Width = 15 },
            [nameof(RawMaterialStockDetailsModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, Width = 18 },
            [nameof(RawMaterialStockDetailsModel.Type)] = new() { DisplayName = "Trans Type", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, Width = 18 },

            // Date fields
            [nameof(RawMaterialStockDetailsModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, Width = 15 },
            // Numeric fields
            [nameof(RawMaterialStockDetailsModel.Quantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 15 },
            [nameof(RawMaterialStockDetailsModel.NetRate)] = new() { DisplayName = "Net Rate", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false, Width = 12 }
        };

        // Define column order for details
        var detailsColumnOrder = new List<string>
        {
            nameof(RawMaterialStockDetailsModel.TransactionDateTime),
            nameof(RawMaterialStockDetailsModel.TransactionNo),
            nameof(RawMaterialStockDetailsModel.Type),
            nameof(RawMaterialStockDetailsModel.RawMaterialName),
            nameof(RawMaterialStockDetailsModel.RawMaterialCode),
            nameof(RawMaterialStockDetailsModel.Quantity),
            nameof(RawMaterialStockDetailsModel.NetRate)
        };

        // Create the details worksheet
        var detailsStream = await ExcelExportUtil.ExportToExcel(
            stockDetailsData,
            "RAW MATERIAL STOCK DETAILS",
            "Transaction Details",
            dateRangeStart,
            dateRangeEnd,
            detailsColumnSettings,
            detailsColumnOrder
        );

        // Now combine both worksheets into one workbook
        return CombineWorksheets(summaryStream, detailsStream);
    }

    /// <summary>
    /// Combine two Excel streams into a single workbook with multiple worksheets
    /// </summary>
    private static MemoryStream CombineWorksheets(MemoryStream summaryStream, MemoryStream detailsStream)
    {
        using var excelEngine = new Syncfusion.XlsIO.ExcelEngine();
        var application = excelEngine.Excel;
        application.DefaultVersion = Syncfusion.XlsIO.ExcelVersion.Xlsx;

        // Load the summary workbook
        var workbook = application.Workbooks.Open(summaryStream);

        // Load the details workbook
        var detailsWorkbook = application.Workbooks.Open(detailsStream);

        // Copy the worksheet from details workbook to main workbook
        workbook.Worksheets.AddCopy(detailsWorkbook.Worksheets[0]);

        // Close the details workbook
        detailsWorkbook.Close();

        // Save the combined workbook to a new stream
        var combinedStream = new MemoryStream();
        workbook.SaveAs(combinedStream);
        combinedStream.Position = 0;

        // Clean up
        summaryStream.Dispose();
        detailsStream.Dispose();

        return combinedStream;
    }
}
