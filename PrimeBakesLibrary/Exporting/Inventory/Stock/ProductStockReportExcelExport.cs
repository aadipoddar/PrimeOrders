using PrimeBakesLibrary.Models.Inventory.Stock;

namespace PrimeBakesLibrary.Exporting.Inventory.Stock;

/// <summary>
/// Excel export functionality for Product Stock Report
/// </summary>
public static class ProductStockReportExcelExport
{
    /// <summary>
    /// Export Product Stock Report to Excel with custom column order and formatting
    /// </summary>
    /// <param name="stockData">Collection of product stock summary records</param>
    /// <param name="dateRangeStart">Start date of the report</param>
    /// <param name="dateRangeEnd">End date of the report</param>
    /// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
    /// <param name="stockDetailsData">Optional collection of product stock detail records for second worksheet</param>
    /// <param name="locationName">Optional location name to display in header</param>
    /// <returns>MemoryStream containing the Excel file</returns>
    public static MemoryStream ExportProductStockReport(
        IEnumerable<ProductStockSummaryModel> stockData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        IEnumerable<ProductStockDetailsModel> stockDetailsData = null,
        string locationName = null)
    {
        // Define custom column settings
        var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
        {
            // IDs - Center aligned, no totals
            [nameof(ProductStockSummaryModel.ProductId)] = new() { DisplayName = "Product ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false, Width = 12 },
            [nameof(ProductStockSummaryModel.ProductCategoryId)] = new() { DisplayName = "Category ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false, Width = 12 },

            // Text fields
            [nameof(ProductStockSummaryModel.ProductName)] = new() { DisplayName = "Product", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, Width = 25 },
            [nameof(ProductStockSummaryModel.ProductCode)] = new() { DisplayName = "Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, Width = 15 },
            [nameof(ProductStockSummaryModel.ProductCategoryName)] = new() { DisplayName = "Category", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, Width = 20 },

            // Stock quantity fields - All with totals
            [nameof(ProductStockSummaryModel.OpeningStock)] = new() { DisplayName = "Opening Stock", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 15 },
            [nameof(ProductStockSummaryModel.PurchaseStock)] = new() { DisplayName = "Purchase Stock", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 15 },
            [nameof(ProductStockSummaryModel.SaleStock)] = new() { DisplayName = "Sale Stock", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 15 },
            [nameof(ProductStockSummaryModel.MonthlyStock)] = new() { DisplayName = "Monthly Stock", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 15 },
            [nameof(ProductStockSummaryModel.ClosingStock)] = new() { DisplayName = "Closing Stock", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 15 },

            [nameof(ProductStockSummaryModel.Rate)] = new() { DisplayName = "Rate", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false, Width = 12 },
            [nameof(ProductStockSummaryModel.ClosingValue)] = new() { DisplayName = "Closing Value", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 15 },

            [nameof(ProductStockSummaryModel.AveragePrice)] = new() { DisplayName = "Average Price", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false, Width = 15 },
            [nameof(ProductStockSummaryModel.WeightedAverageValue)] = new() { DisplayName = "Weighted Avg Value", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 18 },

            [nameof(ProductStockSummaryModel.LastSalePrice)] = new() { DisplayName = "Last Sale Price", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false, Width = 18 },
            [nameof(ProductStockSummaryModel.LastSaleValue)] = new() { DisplayName = "Last Sale Value", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 18 }
        };

        // Define column order based on showAllColumns flag
        List<string> columnOrder;

        // All columns in logical order
        if (showAllColumns)
            columnOrder =
            [
                nameof(ProductStockSummaryModel.ProductName),
                nameof(ProductStockSummaryModel.ProductCode),
                nameof(ProductStockSummaryModel.ProductCategoryName),
                nameof(ProductStockSummaryModel.OpeningStock),
                nameof(ProductStockSummaryModel.PurchaseStock),
                nameof(ProductStockSummaryModel.SaleStock),
                nameof(ProductStockSummaryModel.MonthlyStock),
                nameof(ProductStockSummaryModel.ClosingStock),
                nameof(ProductStockSummaryModel.Rate),
                nameof(ProductStockSummaryModel.ClosingValue),
                nameof(ProductStockSummaryModel.AveragePrice),
                nameof(ProductStockSummaryModel.WeightedAverageValue),
                nameof(ProductStockSummaryModel.LastSalePrice),
                nameof(ProductStockSummaryModel.LastSaleValue)
            ];

        // Summary columns only (key information)
        else
            columnOrder =
            [
                nameof(ProductStockSummaryModel.ProductName),
                nameof(ProductStockSummaryModel.OpeningStock),
                nameof(ProductStockSummaryModel.PurchaseStock),
                nameof(ProductStockSummaryModel.SaleStock),
                nameof(ProductStockSummaryModel.ClosingStock),
                nameof(ProductStockSummaryModel.Rate),
                nameof(ProductStockSummaryModel.ClosingValue)
            ];

        // If no details data provided, use the simple single-worksheet export
        if (stockDetailsData == null || !stockDetailsData.Any())
        {
            return ExcelExportUtil.ExportToExcel(
                stockData,
                "PRODUCT STOCK REPORT",
                "Stock Summary",
                dateRangeStart,
                dateRangeEnd,
                columnSettings,
                columnOrder,
                locationName
            );
        }

        // Multi-worksheet export
        return ExportWithDetails(
            stockData,
            stockDetailsData,
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            locationName
        );
    }

    /// <summary>
    /// Export with both summary and details worksheets
    /// </summary>
    private static MemoryStream ExportWithDetails(
        IEnumerable<ProductStockSummaryModel> stockData,
        IEnumerable<ProductStockDetailsModel> stockDetailsData,
        DateOnly? dateRangeStart,
        DateOnly? dateRangeEnd,
        Dictionary<string, ExcelExportUtil.ColumnSetting> summaryColumnSettings,
        List<string> summaryColumnOrder,
        string locationName = null)
    {
        // Create the first worksheet with summary data
        var summaryStream = ExcelExportUtil.ExportToExcel(
            stockData,
            "PRODUCT STOCK REPORT",
            "Stock Summary",
            dateRangeStart,
            dateRangeEnd,
            summaryColumnSettings,
            summaryColumnOrder,
            locationName
        );

        // Define column settings for details worksheet
        var detailsColumnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
        {
            // IDs - Center aligned, no totals
            [nameof(ProductStockDetailsModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false, Width = 10 },
            [nameof(ProductStockDetailsModel.ProductId)] = new() { DisplayName = "Product ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false, Width = 12 },
            [nameof(ProductStockDetailsModel.TransactionId)] = new() { DisplayName = "Transaction ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false, Width = 15 },
            [nameof(ProductStockDetailsModel.LocationId)] = new() { DisplayName = "Location ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false, Width = 12 },

            // Text fields
            [nameof(ProductStockDetailsModel.ProductName)] = new() { DisplayName = "Product", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, Width = 25 },
            [nameof(ProductStockDetailsModel.ProductCode)] = new() { DisplayName = "Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, Width = 15 },
            [nameof(ProductStockDetailsModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, Width = 18 },
            [nameof(ProductStockDetailsModel.Type)] = new() { DisplayName = "Trans Type", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, Width = 18 },

            // Date fields
            [nameof(ProductStockDetailsModel.TransactionDate)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, Width = 15 },

            // Numeric fields
            [nameof(ProductStockDetailsModel.Quantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 15 },
            [nameof(ProductStockDetailsModel.NetRate)] = new() { DisplayName = "Net Rate", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false, Width = 12 }
        };

        // Define column order for details
        var detailsColumnOrder = new List<string>
        {
            nameof(ProductStockDetailsModel.TransactionDate),
            nameof(ProductStockDetailsModel.TransactionNo),
            nameof(ProductStockDetailsModel.Type),
            nameof(ProductStockDetailsModel.ProductName),
            nameof(ProductStockDetailsModel.ProductCode),
            nameof(ProductStockDetailsModel.Quantity),
            nameof(ProductStockDetailsModel.NetRate)
        };

        // Create the details worksheet
        var detailsStream = ExcelExportUtil.ExportToExcel(
            stockDetailsData,
            "PRODUCT STOCK DETAILS",
            "Transaction Details",
            dateRangeStart,
            dateRangeEnd,
            detailsColumnSettings,
            detailsColumnOrder,
            locationName
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
