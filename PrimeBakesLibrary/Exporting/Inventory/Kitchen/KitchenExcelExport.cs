using PrimeBakesLibrary.Models.Inventory.Kitchen;

namespace PrimeBakesLibrary.Exporting.Inventory.Kitchen;

/// <summary>
/// Excel export functionality for Kitchen
/// </summary>
public static class KitchenExcelExport
{
    /// <summary>
    /// Export Kitchen data to Excel with custom column order and formatting
    /// </summary>
    /// <param name="kitchenData">Collection of kitchen records</param>
    /// <returns>MemoryStream containing the Excel file</returns>
    public static async Task<MemoryStream> ExportKitchen(IEnumerable<KitchenModel> kitchenData)
    {
        // Create enriched data with status formatting
        var enrichedData = kitchenData.Select(kitchen => new
        {
            kitchen.Id,
            kitchen.Name,
            kitchen.Remarks,
            Status = kitchen.Status ? "Active" : "Deleted"
        });

        // Define custom column settings
        var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
        {
            // ID - Center aligned, no totals
            [nameof(KitchenModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Text fields - Left aligned
            [nameof(KitchenModel.Name)] = new() { DisplayName = "Kitchen Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
            [nameof(KitchenModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

            // Status - Center aligned
            [nameof(KitchenModel.Status)] = new() { DisplayName = "Status", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
        };

        // Define column order
        List<string> columnOrder =
        [
            nameof(KitchenModel.Id),
            nameof(KitchenModel.Name),
            nameof(KitchenModel.Remarks),
            nameof(KitchenModel.Status)
        ];

        // Call the generic Excel export utility
        return await ExcelExportUtil.ExportToExcel(
            enrichedData,
            "KITCHEN MASTER",
            "Kitchen Data",
            null,
            null,
            columnSettings,
            columnOrder
        );
    }
}
