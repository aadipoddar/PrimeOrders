using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.Masters;

/// <summary>
/// Excel export functionality for Financial Year
/// </summary>
public static class FinancialYearExcelExport
{
    /// <summary>
    /// Export Financial Year data to Excel with custom column order and formatting
    /// </summary>
    /// <param name="financialYearData">Collection of financial year records</param>
    /// <returns>MemoryStream containing the Excel file</returns>
    public static async Task<MemoryStream> ExportFinancialYear(IEnumerable<FinancialYearModel> financialYearData)
    {
        // Create enriched data with status formatting
        var enrichedData = financialYearData.Select(fy => new
        {
            fy.Id,
            StartDate = fy.StartDate.ToString("dd-MMM-yyyy"),
            EndDate = fy.EndDate.ToString("dd-MMM-yyyy"),
            fy.YearNo,
            fy.Remarks,
            Locked = fy.Locked ? "Yes" : "No",
            Status = fy.Status ? "Active" : "Deleted"
        });

        // Define custom column settings
        var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
        {
            // ID - Center aligned, no totals
            [nameof(FinancialYearModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Date fields - Center aligned
            [nameof(FinancialYearModel.StartDate)] = new() { DisplayName = "Start Date", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(FinancialYearModel.EndDate)] = new() { DisplayName = "End Date", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Year No - Center aligned
            [nameof(FinancialYearModel.YearNo)] = new() { DisplayName = "Year No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Text fields - Left aligned
            [nameof(FinancialYearModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

            // Locked - Center aligned
            [nameof(FinancialYearModel.Locked)] = new() { DisplayName = "Locked", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Status - Center aligned
            [nameof(FinancialYearModel.Status)] = new() { DisplayName = "Status", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
        };

        // Define column order
        List<string> columnOrder =
        [
            nameof(FinancialYearModel.Id),
            nameof(FinancialYearModel.StartDate),
            nameof(FinancialYearModel.EndDate),
            nameof(FinancialYearModel.YearNo),
            nameof(FinancialYearModel.Remarks),
            nameof(FinancialYearModel.Locked),
            nameof(FinancialYearModel.Status)
		];

        // Call the generic Excel export utility
        return await ExcelReportExportUtil.ExportToExcel(
            enrichedData,
            "FINANCIAL YEAR",
            "Financial Year Data",
            null,
            null,
            columnSettings,
            columnOrder
        );
    }
}
