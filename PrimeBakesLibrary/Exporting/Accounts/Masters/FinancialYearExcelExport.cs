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
    public static MemoryStream ExportFinancialYear(IEnumerable<FinancialYearModel> financialYearData)
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
        var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
        {
            // ID - Center aligned, no totals
            ["Id"] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Date fields - Center aligned
            ["StartDate"] = new() { DisplayName = "Start Date", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            ["EndDate"] = new() { DisplayName = "End Date", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Year No - Center aligned
            ["YearNo"] = new() { DisplayName = "Year No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Text fields - Left aligned
            ["Remarks"] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

            // Locked - Center aligned
            ["Locked"] = new() { DisplayName = "Locked", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Status - Center aligned
            ["Status"] = new() { DisplayName = "Status", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
        };

        // Define column order
        List<string> columnOrder =
        [
            "Id", "StartDate", "EndDate", "YearNo", "Remarks", "Locked", "Status"
        ];

        // Call the generic Excel export utility
        return ExcelExportUtil.ExportToExcel(
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
