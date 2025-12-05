using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Inventory;

namespace PrimeBakesLibrary.Exporting.Inventory.RawMaterial;

/// <summary>
/// Excel export functionality for Raw Material
/// </summary>
public static class RawMaterialExcelExport
{
    /// <summary>
    /// Export Raw Material data to Excel with custom column order and formatting
    /// </summary>
    /// <param name="rawMaterialData">Collection of raw material records</param>
    /// <returns>MemoryStream containing the Excel file</returns>
    public static async Task<MemoryStream> ExportRawMaterial<T>(IEnumerable<T> rawMaterialData)
    {
        // Create enriched data with status formatting
        var enrichedData = rawMaterialData.Select(rm =>
        {
            var props = typeof(T).GetProperties();
            var id = props.FirstOrDefault(p => p.Name == "Id")?.GetValue(rm);
            var name = props.FirstOrDefault(p => p.Name == "Name")?.GetValue(rm)?.ToString();
            var code = props.FirstOrDefault(p => p.Name == "Code")?.GetValue(rm)?.ToString();
            var category = props.FirstOrDefault(p => p.Name == "Category")?.GetValue(rm)?.ToString();
            var rate = props.FirstOrDefault(p => p.Name == "Rate")?.GetValue(rm);
            var unit = props.FirstOrDefault(p => p.Name == "UnitOfMeasurement")?.GetValue(rm)?.ToString();
            var tax = props.FirstOrDefault(p => p.Name == "Tax")?.GetValue(rm)?.ToString();
            var remarks = props.FirstOrDefault(p => p.Name == "Remarks")?.GetValue(rm)?.ToString();
            var status = props.FirstOrDefault(p => p.Name == "Status")?.GetValue(rm);

            return new
            {
                Id = id,
                Name = name,
                Code = code,
                Category = category,
                Rate = rate,
                UnitOfMeasurement = unit,
                Tax = tax,
                Remarks = remarks,
                Status = status is bool statusBool && statusBool ? "Active" : "Deleted"
            };
        });

        // Define custom column settings
        var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
        {
            // ID - Center aligned, no totals
            [nameof(RawMaterialModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Name - Left aligned
            [nameof(RawMaterialModel.Name)] = new() { DisplayName = "Raw Material Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },

            // Code - Left aligned
            [nameof(RawMaterialModel.Code)] = new() { DisplayName = "Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },

            // Category - Left aligned
            ["Category"] = new() { DisplayName = "Category", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },

            // Rate - Right aligned with 2 decimal places
            [nameof(RawMaterialModel.Rate)] = new() { DisplayName = "Rate", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, Format = "0.00", IncludeInTotal = false },

            // Unit - Center aligned
            [nameof(RawMaterialModel.UnitOfMeasurement)] = new() { DisplayName = "Unit", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Tax - Center aligned
            ["Tax"] = new() { DisplayName = "Tax Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Remarks - Left aligned
            [nameof(RawMaterialModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

            // Status - Center aligned
            [nameof(RawMaterialModel.Status)] = new() { DisplayName = "Status", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
        };

        // Define column order
        List<string> columnOrder =
        [
            nameof(RawMaterialModel.Id),
            nameof(RawMaterialModel.Name),
            nameof(RawMaterialModel.Code),
			"Category",
            nameof(RawMaterialModel.Rate),
            nameof(RawMaterialModel.UnitOfMeasurement),
            "Tax",
            nameof(RawMaterialModel.Remarks),
            nameof(RawMaterialModel.Status)
        ];

        // Call the generic Excel export utility
        return await ExcelReportExportUtil.ExportToExcel(
            enrichedData,
            "RAW MATERIAL MASTER",
            "Raw Material Data",
            null,
            null,
            columnSettings,
            columnOrder
        );
    }
}
