using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.Masters;

/// <summary>
/// PDF export functionality for Group
/// </summary>
public static class GroupPDFExport
{
	/// <summary>
	/// Export group data to PDF with custom column order and formatting
	/// </summary>
	/// <param name="groupData">Collection of group records</param>
	/// <returns>MemoryStream containing the PDF file</returns>
	public static async Task<MemoryStream> ExportGroup(IEnumerable<GroupModel> groupData)
	{
		// Create enriched data with status formatting
		var enrichedData = groupData.Select(group => new
		{
			group.Id,
			group.Name,
			group.Remarks,
			Status = group.Status ? "Active" : "Deleted"
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

			["Name"] = new() { DisplayName = "Group Name", IncludeInTotal = false },
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
			"Id", "Name", "Remarks", "Status"
		];

		// Call the generic PDF export utility
		return await PDFReportExportUtil.ExportToPdf(
			enrichedData,
			"GROUP MASTER",
			null,
			null,
			columnSettings,
			columnOrder,
			useLandscape: false
		);
	}
}
