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
            ["Id"] = new()
            {
                DisplayName = "ID",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            ["Name"] = new() { DisplayName = "User Name", IncludeInTotal = false },
            ["Passcode"] = new()
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

            ["Sales"] = new()
            {
                DisplayName = "Sales",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            ["Order"] = new()
            {
                DisplayName = "Order",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            ["Inventory"] = new()
            {
                DisplayName = "Inventory",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            ["Accounts"] = new()
            {
                DisplayName = "Accounts",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            ["Admin"] = new()
            {
                DisplayName = "Admin",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            ["Remarks"] = new() { DisplayName = "Remarks", IncludeInTotal = false },

            ["Status"] = new()
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
            "Id", "Name", "Passcode", "Location", "Sales", "Order", "Inventory", "Accounts", "Admin", "Remarks", "Status"
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