using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Data.Common;

namespace PrimeBakesLibrary.Exporting.Operations;

/// <summary>
/// Excel export functionality for User
/// </summary>
public static class UserExcelExport
{
    /// <summary>
    /// Export User data to Excel with custom column order and formatting
    /// </summary>
    /// <param name="userData">Collection of user records</param>
    /// <returns>MemoryStream containing the Excel file</returns>
    public static async Task<MemoryStream> ExportUser(IEnumerable<UserModel> userData)
    {
        // Load locations to display location names instead of IDs
        var locations = CommonData.LoadTableData<LocationModel>(TableNames.Location).Result;

        // Create enriched data with location names
        var enrichedData = userData.Select(user => new
        {
            user.Id,
            user.Name,
            Passcode = user.Passcode.ToString("0000"),
            Location = locations.FirstOrDefault(l => l.Id == user.LocationId)?.Name ?? "N/A",
            Sales = user.Sales ? "Yes" : "No",
            Order = user.Order ? "Yes" : "No",
            Inventory = user.Inventory ? "Yes" : "No",
            Accounts = user.Accounts ? "Yes" : "No",
            Admin = user.Admin ? "Yes" : "No",
            user.Remarks,
            Status = user.Status ? "Active" : "Deleted"
        });

        // Define custom column settings
        var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
        {
            // IDs - Center aligned, no totals
            ["Id"] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Text fields - Left aligned
            ["Name"] = new() { DisplayName = "User Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
            ["Passcode"] = new() { DisplayName = "Passcode", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IsRequired = true },
            ["Location"] = new() { DisplayName = "Location", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            ["Remarks"] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

            // Permissions - Center aligned
            ["Sales"] = new() { DisplayName = "Sales", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            ["Order"] = new() { DisplayName = "Order", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            ["Inventory"] = new() { DisplayName = "Inventory", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            ["Accounts"] = new() { DisplayName = "Accounts", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            ["Admin"] = new() { DisplayName = "Admin", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Status - Center aligned
            ["Status"] = new() { DisplayName = "Status", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
        };

        // Define column order
        List<string> columnOrder =
        [
            "Id", "Name", "Passcode", "Location", "Sales", "Order", "Inventory", "Accounts", "Admin", "Remarks", "Status"
        ];

        // Call the generic Excel export utility
        return await ExcelExportUtil.ExportToExcel(
            enrichedData,
            "USER MASTER",
            "User Data",
            null,
            null,
            columnSettings,
            columnOrder
        );
    }
}