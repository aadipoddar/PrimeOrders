using PrimeBakesLibrary.Models.Common;

namespace PrimeBakesLibrary.Exporting.Operations;

/// <summary>
/// Excel export functionality for Location
/// </summary>
public static class LocationExcelExport
{
    /// <summary>
    /// Export Location data to Excel with custom column order and formatting
    /// </summary>
    /// <param name="locationData">Collection of location records</param>
    /// <returns>MemoryStream containing the Excel file</returns>
    public static async Task<MemoryStream> ExportLocation(IEnumerable<LocationModel> locationData)
    {
        // Define custom column settings
        var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
        {
            // IDs - Center aligned, no totals
            ["Id"] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Text fields - Left aligned
            ["Name"] = new() { DisplayName = "Location Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
            ["PrefixCode"] = new() { DisplayName = "Prefix Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IsRequired = true },
            ["Remarks"] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

            // Numeric fields - Right aligned
            ["Discount"] = new() { DisplayName = "Discount %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false },

            // Status - Center aligned
            ["Status"] = new() { DisplayName = "Status", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
        };

        // Define column order
        List<string> columnOrder =
        [
            "Id", "Name", "PrefixCode", "Discount", "Remarks", "Status"
        ];

        // Call the generic Excel export utility
        return await ExcelExportUtil.ExportToExcel(
            locationData,
            "LOCATION MASTER",
            "Location Data",
            null,
            null,
            columnSettings,
            columnOrder
        );
    }
}
