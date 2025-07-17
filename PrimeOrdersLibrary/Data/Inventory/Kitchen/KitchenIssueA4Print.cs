using PrimeOrdersLibrary.Data.Common;
using PrimeOrdersLibrary.Exporting;
using PrimeOrdersLibrary.Models.Inventory;

using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;

namespace PrimeOrdersLibrary.Data.Inventory.Kitchen;

public static class KitchenIssueA4Print
{
	public static async Task<MemoryStream> GenerateA4KitchenIssueBill(int kitchenIssueId)
	{
		var kitchenIssue = await CommonData.LoadTableDataById<KitchenIssueModel>(TableNames.KitchenIssue, kitchenIssueId);
		var kitchen = await CommonData.LoadTableDataById<KitchenModel>(TableNames.Kitchen, kitchenIssue.KitchenId);
		var user = await CommonData.LoadTableDataById<UserModel>(TableNames.User, kitchenIssue.UserId);
		var kitchenIssueDetails = await KitchenIssueData.LoadKitchenIssueDetailByKitchenIssue(kitchenIssueId);

		List<KitchenIssueRawMaterialCartModel> kitchenIssueDetailCartModel = [];
		foreach (var item in kitchenIssueDetails)
		{
			var rawMaterial = await CommonData.LoadTableDataById<RawMaterialModel>(TableNames.RawMaterial, item.RawMaterialId);
			kitchenIssueDetailCartModel.Add(new()
			{
				RawMaterialId = item.RawMaterialId,
				RawMaterialName = rawMaterial.Name,
				Quantity = item.Quantity,
				Rate = rawMaterial.MRP,
				Total = item.Quantity * rawMaterial.MRP
			});
		}

		var (pdfDocument, pdfPage) = PDFExportUtil.CreateA4Document($"Kitchen Issue - {kitchen.Name}");

		float currentY = PDFExportUtil.DrawCompanyInformation(pdfPage, "KITCHEN ISSUE");
		currentY = DrawIssueDetails(pdfPage, currentY, kitchenIssue, kitchen, user);
		var result = DrawItemDetails(pdfPage, currentY, kitchenIssueDetailCartModel);
		DrawSummary(pdfDocument, result.Page, result.Bounds.Bottom + 20, kitchenIssueDetailCartModel);

		return PDFExportUtil.FinalizePdfDocument(pdfDocument);
	}

	private static float DrawIssueDetails(PdfPage pdfPage, float currentY, KitchenIssueModel kitchenIssue, KitchenModel kitchen, UserModel user)
	{
		var leftColumnDetails = new Dictionary<string, string>
		{
			["Trans No"] = kitchenIssue.TransactionNo ?? "N/A",
			["Date"] = kitchenIssue.IssueDate.ToString("dddd, MMMM dd, yyyy hh:mm tt"),
			["Issued By"] = user.Name ?? "N/A"
		};

		var rightColumnDetails = new Dictionary<string, string>
		{
			["Kitchen"] = kitchen.Name ?? "N/A"
		};

		return PDFExportUtil.DrawInvoiceDetailsSection(pdfPage, currentY, "Issue Details", leftColumnDetails, rightColumnDetails);
	}

	private static PdfGridLayoutResult DrawItemDetails(PdfPage pdfPage, float currentY, List<KitchenIssueRawMaterialCartModel> kitchenIssueDetails)
	{
		var dataSource = kitchenIssueDetails.Select((item, index) => new
		{
			SNo = index + 1,
			Name = item.RawMaterialName.ToString(),
			Qty = item.Quantity.ToString("N2"),
			Unit = "KG", // Default unit for raw materials
			Total = item.Total.ToString("N2")
		}).ToList();

		var tableWidth = pdfPage.GetClientSize().Width - PDFExportUtil._pageMargin * 2;
		var columnWidths = new float[]
		{
			tableWidth * 0.08f, // S.No
			tableWidth * 0.45f, // Name
			tableWidth * 0.15f, // Qty
			tableWidth * 0.12f, // Unit
			tableWidth * 0.20f  // Total
		};

		var columnAlignments = new PdfTextAlignment[]
		{
			PdfTextAlignment.Center, // S.No
			PdfTextAlignment.Left,   // Name
			PdfTextAlignment.Center, // Qty
			PdfTextAlignment.Center, // Unit
			PdfTextAlignment.Right   // Total
		};

		var pdfGrid = PDFExportUtil.CreateStyledGrid(dataSource, columnWidths, columnAlignments);

		var result = pdfGrid.Draw(pdfPage, new RectangleF(PDFExportUtil._pageMargin, currentY, tableWidth,
			pdfPage.GetClientSize().Height - currentY - PDFExportUtil._pageMargin));

		return result;
	}

	private static float DrawSummary(PdfDocument pdfDocument, PdfPage pdfPage, float currentY, List<KitchenIssueRawMaterialCartModel> kitchenIssueDetails)
	{
		var summaryItems = new Dictionary<string, string>
		{
			["Total Items: "] = kitchenIssueDetails.Count.ToString(),
			["Total Quantity: "] = kitchenIssueDetails.Sum(x => x.Quantity).ToString("N2"),
			["Total Value: "] = $"₹{kitchenIssueDetails.Sum(x => x.Total):N2}"
		};

		return PDFExportUtil.DrawSummarySection(pdfDocument, pdfPage, currentY, summaryItems, kitchenIssueDetails.Sum(x => x.Total));
	}
}