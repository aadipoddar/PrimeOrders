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
        var locations = await CommonData.LoadTableData<LocationModel>(TableNames.Location);

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
            [nameof(UserModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Text fields - Left aligned
            [nameof(UserModel.Name)] = new() { DisplayName = "User Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
            [nameof(UserModel.Passcode)] = new() { DisplayName = "Passcode", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IsRequired = true },
            ["Location"] = new() { DisplayName = "Location", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(UserModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

            // Permissions - Center aligned
            [nameof(UserModel.Sales)] = new() { DisplayName = "Sales", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(UserModel.Order)] = new() { DisplayName = "Order", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(UserModel.Inventory)] = new() { DisplayName = "Inventory", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(UserModel.Accounts)] = new() { DisplayName = "Accounts", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(UserModel.Admin)] = new() { DisplayName = "Admin", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Status - Center aligned
            [nameof(UserModel.Status)] = new() { DisplayName = "Status", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
        };

        // Define column order
        List<string> columnOrder =
        [
            nameof(UserModel.Id),
            nameof(UserModel.Name),
            nameof(UserModel.Passcode),
			"Location",
			nameof(UserModel.Sales),
            nameof(UserModel.Order),
            nameof(UserModel.Inventory),
            nameof(UserModel.Accounts),
            nameof(UserModel.Admin),
            nameof(UserModel.Remarks),
            nameof(UserModel.Status)
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