using PrimeBakesLibrary.Models.Sales.Order;

namespace PrimeBakesLibrary.Exporting.Sales.Order;

/// <summary>
/// Excel export functionality for Order Report
/// </summary>
public static class OrderReportExcelExport
{
    /// <summary>
    /// Export Order Report to Excel with custom column order and formatting
    /// </summary>
    /// <param name="orderData">Collection of order overview records</param>
    /// <param name="dateRangeStart">Start date of the report</param>
    /// <param name="dateRangeEnd">End date of the report</param>
    /// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
    /// <param name="showLocation">Whether to include location column</param>
    /// <param name="locationName">Name of the location for report header</param>
    /// <returns>MemoryStream containing the Excel file</returns>
    public static async Task<MemoryStream> ExportOrderReport(
        IEnumerable<OrderOverviewModel> orderData,
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
            [nameof(OrderOverviewModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(OrderOverviewModel.CompanyId)] = new() { DisplayName = "Company ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(OrderOverviewModel.LocationId)] = new() { DisplayName = "Location ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(OrderOverviewModel.SaleId)] = new() { DisplayName = "Sale ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(OrderOverviewModel.FinancialYearId)] = new() { DisplayName = "Financial Year ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(OrderOverviewModel.CreatedBy)] = new() { DisplayName = "Created By ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(OrderOverviewModel.LastModifiedBy)] = new() { DisplayName = "Modified By ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Text fields - Left aligned
            [nameof(OrderOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(OrderOverviewModel.SaleTransactionNo)] = new() { DisplayName = "Sale Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(OrderOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(OrderOverviewModel.LocationName)] = new() { DisplayName = "Location", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(OrderOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(OrderOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(OrderOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(OrderOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(OrderOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(OrderOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

            // Dates - Center aligned with custom format
            [nameof(OrderOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            [nameof(OrderOverviewModel.SaleDateTime)] = new() { DisplayName = "Sale Date", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            [nameof(OrderOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            [nameof(OrderOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },

            // Numeric fields - Right aligned with totals
            [nameof(OrderOverviewModel.TotalItems)] = new() { DisplayName = "Items", Format = "#,##0", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(OrderOverviewModel.TotalQuantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true }
        };

        // Define column order based on visibility setting
        List<string> columnOrder;

        if (showAllColumns)
        {
            // All columns - detailed view
            columnOrder =
            [
                nameof(OrderOverviewModel.TransactionNo),
                nameof(OrderOverviewModel.SaleTransactionNo),
				nameof(OrderOverviewModel.CompanyName)
            ];

            // Add location columns if showLocation is true
            if (showLocation)
                columnOrder.Add(nameof(OrderOverviewModel.LocationName));

            // Continue with remaining columns
            columnOrder.AddRange(
            [
                nameof(OrderOverviewModel.TransactionDateTime),
                nameof(OrderOverviewModel.SaleDateTime),
                nameof(OrderOverviewModel.FinancialYear),
                nameof(OrderOverviewModel.TotalItems),
                nameof(OrderOverviewModel.TotalQuantity),
                nameof(OrderOverviewModel.Remarks),
                nameof(OrderOverviewModel.CreatedByName),
                nameof(OrderOverviewModel.CreatedAt),
                nameof(OrderOverviewModel.CreatedFromPlatform),
                nameof(OrderOverviewModel.LastModifiedByUserName),
                nameof(OrderOverviewModel.LastModifiedAt),
				nameof(OrderOverviewModel.LastModifiedFromPlatform)
            ]);
        }
        else
        {
            // Summary columns - key fields only
            columnOrder =
            [
                nameof(OrderOverviewModel.TransactionNo),       
                nameof(OrderOverviewModel.SaleTransactionNo),
                nameof(OrderOverviewModel.TransactionDateTime),
                nameof(OrderOverviewModel.LocationName),
                nameof(OrderOverviewModel.TotalItems),
				nameof(OrderOverviewModel.TotalQuantity)
            ];
        }

        // Call the generic Excel export utility
        return await ExcelExportUtil.ExportToExcel(
            orderData,
            "ORDER REPORT",
            "Order Transactions",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            locationName: locationName
        );
    }
}
