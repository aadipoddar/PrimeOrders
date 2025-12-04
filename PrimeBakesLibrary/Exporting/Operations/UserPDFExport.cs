using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Data.Common;

namespace PrimeBakesLibrary.Exporting.Operations;

/// <summary>
/// PDF export functionality for User
/// </summary>
public static class UserPDFExport
{
    /// <summary>
    /// Export User data to PDF with custom column order and formatting
    /// </summary>
    /// <param name="userData">Collection of user records</param>
    /// <returns>MemoryStream containing the PDF file</returns>
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
        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
        {
            [nameof(UserModel.Id)] = new()
            {
                DisplayName = "ID",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(UserModel.Name)] = new() { DisplayName = "User Name", IncludeInTotal = false },
            [nameof(UserModel.Passcode)] = new()
            {
                DisplayName = "Passcode",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },
            ["Location"] = new() { DisplayName = "Location", IncludeInTotal = false },

            [nameof(UserModel.Sales)] = new()
            {
                DisplayName = "Sales",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(UserModel.Order)] = new()
            {
                DisplayName = "Order",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(UserModel.Inventory)] = new()
            {
                DisplayName = "Inventory",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(UserModel.Accounts)] = new()
            {
                DisplayName = "Accounts",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(UserModel.Admin)] = new()
            {
                DisplayName = "Admin",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(UserModel.Remarks)] = new() { DisplayName = "Remarks", IncludeInTotal = false },

            [nameof(UserModel.Status)] = new()
            {
                DisplayName = "Status",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            }
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

        // Call the generic PDF export utility
        return await PDFReportExportUtil.ExportToPdf(
            enrichedData,
            "USER MASTER",
            null,
            null,
            columnSettings,
            columnOrder,
            useLandscape: true
        );
    }
}