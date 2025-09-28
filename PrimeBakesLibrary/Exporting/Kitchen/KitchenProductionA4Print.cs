using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Product;

using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;

namespace PrimeBakesLibrary.Exporting.Kitchen;

public static class KitchenProductionA4Print
{
	public static async Task<MemoryStream> GenerateA4KitchenProductionBill(int kitchenProductionId)
	{
		var kitchenProduction = await CommonData.LoadTableDataById<KitchenProductionModel>(TableNames.KitchenProduction, kitchenProductionId);
		var kitchen = await CommonData.LoadTableDataById<KitchenModel>(TableNames.Kitchen, kitchenProduction.KitchenId);
		var user = await CommonData.LoadTableDataById<UserModel>(TableNames.User, kitchenProduction.UserId);
		var kitchenProductionDetails = await KitchenProductionData.LoadKitchenProductionDetailByKitchenProduction(kitchenProductionId);

		List<KitchenProductionProductCartModel> kitchenProductionDetailCartModel = [];
		foreach (var item in kitchenProductionDetails)
		{
			var product = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, item.ProductId);
			kitchenProductionDetailCartModel.Add(new()
			{
				ProductId = item.ProductId,
				ProductName = product.Name,
				Quantity = item.Quantity,
				Rate = product.Rate,
				Total = item.Quantity * product.Rate
			});
		}

		var (pdfDocument, pdfPage) = PDFExportUtil.CreateA4Document($"Kitchen Production - {kitchen.Name}");

		float currentY = PDFExportUtil.DrawCompanyInformation(pdfPage, "KITCHEN PRODUCTION");
		currentY = DrawProductionDetails(pdfPage, currentY, kitchenProduction, kitchen, user);
		var result = DrawItemDetails(pdfPage, currentY, kitchenProductionDetailCartModel);
		DrawSummary(pdfDocument, result.Page, result.Bounds.Bottom + 20, kitchenProductionDetailCartModel);

		return PDFExportUtil.FinalizePdfDocument(pdfDocument);
	}

	private static float DrawProductionDetails(PdfPage pdfPage, float currentY, KitchenProductionModel kitchenProduction, KitchenModel kitchen, UserModel user)
	{
		var leftColumnDetails = new Dictionary<string, string>
		{
			["Trans No"] = kitchenProduction.TransactionNo ?? "N/A",
			["Date"] = kitchenProduction.ProductionDate.ToString("dddd, MMMM dd, yyyy hh:mm tt"),
			["User"] = user.Name ?? "N/A"
		};

		var rightColumnDetails = new Dictionary<string, string>
		{
			["Kitchen"] = kitchen.Name ?? "N/A"
		};

		return PDFExportUtil.DrawInvoiceDetailsSection(pdfPage, currentY, "Production Details", leftColumnDetails, rightColumnDetails);
	}

	private static PdfGridLayoutResult DrawItemDetails(PdfPage pdfPage, float currentY, List<KitchenProductionProductCartModel> kitchenProductionDetails)
	{
		var dataSource = kitchenProductionDetails.Select((item, index) => new
		{
			SNo = index + 1,
			Name = item.ProductName.ToString(),
			Qty = item.Quantity.ToString("N2"),
			Unit = "PCS", // Default unit for products
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

	private static float DrawSummary(PdfDocument pdfDocument, PdfPage pdfPage, float currentY, List<KitchenProductionProductCartModel> kitchenProductionDetails)
	{
		var summaryItems = new Dictionary<string, string>
		{
			["Total Items: "] = kitchenProductionDetails.Count.ToString(),
			["Total Quantity: "] = kitchenProductionDetails.Sum(x => x.Quantity).ToString("N2"),
			["Total Value: "] = $"₹{kitchenProductionDetails.Sum(x => x.Total):N2}"
		};

		return PDFExportUtil.DrawSummarySection(pdfDocument, pdfPage, currentY, summaryItems, kitchenProductionDetails.Sum(x => x.Total));
	}
}