using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Sale;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Product;
using PrimeBakesLibrary.Models.Sale;

using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;

namespace PrimeBakesLibrary.Exporting.Sale;

public static class SaleReturnA4Print
{
	public static async Task<MemoryStream> GenerateA4SaleReturnBill(int saleReturnId)
	{
		var saleReturn = await CommonData.LoadTableDataById<SaleReturnModel>(TableNames.SaleReturn, saleReturnId);
		var saleReturnOverview = (await SaleReturnData.LoadSaleReturnDetailsByDateLocationId(
			saleReturn.ReturnDateTime.AddDays(-1),
			saleReturn.ReturnDateTime.AddDays(1),
			saleReturn.LocationId))
			.FirstOrDefault(x => x.SaleReturnId == saleReturnId);

		var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, saleReturn.LocationId);
		var originalSale = await CommonData.LoadTableDataById<SaleModel>(TableNames.Sale, saleReturn.SaleId);
		var saleReturnDetails = await SaleReturnData.LoadSaleReturnDetailBySaleReturn(saleReturnId);

		List<SaleReturnProductCartModel> saleReturnProductCartModel = [];
		foreach (var item in saleReturnDetails)
		{
			var product = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, item.ProductId);
			saleReturnProductCartModel.Add(new()
			{
				ProductId = item.ProductId,
				ProductName = product.Name,
				Quantity = item.Quantity,
				MaxQuantity = 0, // Not needed for printing
				SoldQuantity = 0, // Not needed for printing
				AlreadyReturnedQuantity = 0 // Not needed for printing
			});
		}

		var (pdfDocument, pdfPage) = PDFExportUtil.CreateA4Document();

		float currentY = await PDFExportUtil.DrawCompanyInformation(pdfPage, "SALE RETURN INVOICE");
		currentY = DrawInvoiceDetails(pdfPage, currentY, saleReturnOverview, originalSale);
		var result = DrawItemDetails(pdfPage, currentY, saleReturnProductCartModel);
		DrawSummary(pdfDocument, result.Page, result.Bounds.Bottom + 20, saleReturnOverview);

		return PDFExportUtil.FinalizePdfDocument(pdfDocument);
	}

	private static float DrawInvoiceDetails(PdfPage pdfPage, float currentY, SaleReturnOverviewModel saleReturn, SaleModel originalSale)
	{
		var leftColumnDetails = new Dictionary<string, string>
		{
			["Trans No"] = saleReturn.TransactionNo ?? "N/A",
			["Return Date"] = saleReturn.ReturnDateTime.ToString("dddd, MMMM dd, yyyy hh:mm tt"),
			["User"] = saleReturn.UserName ?? "N/A"
		};

		var rightColumnDetails = new Dictionary<string, string>
		{
			["Sale No"] = saleReturn.OriginalBillNo ?? "N/A",
			["Location"] = saleReturn.LocationName ?? "N/A"
		};

		if (!string.IsNullOrEmpty(saleReturn.Remarks))
			rightColumnDetails["Remarks"] = saleReturn.Remarks;

		return PDFExportUtil.DrawInvoiceDetailsSection(pdfPage, currentY, "Return Details", leftColumnDetails, rightColumnDetails);
	}

	private static PdfGridLayoutResult DrawItemDetails(PdfPage pdfPage, float currentY, List<SaleReturnProductCartModel> saleReturnDetails)
	{
		var dataSource = saleReturnDetails.Select((item, index) => new
		{
			SNo = index + 1,
			Name = item.ProductName.ToString(),
			Qty = item.Quantity.ToString("N2")
		}).ToList();

		var tableWidth = pdfPage.GetClientSize().Width - PDFExportUtil._pageMargin * 2;
		var columnWidths = new float[]
		{
			tableWidth * 0.15f, // S.No
			tableWidth * 0.65f, // Name
			tableWidth * 0.20f  // Qty
		};

		var columnAlignments = new PdfTextAlignment[]
		{
			PdfTextAlignment.Center, // S.No
			PdfTextAlignment.Left,   // Name
			PdfTextAlignment.Center  // Qty
		};

		var pdfGrid = PDFExportUtil.CreateStyledGrid(dataSource, columnWidths, columnAlignments);

		var result = pdfGrid.Draw(pdfPage, new RectangleF(PDFExportUtil._pageMargin, currentY, tableWidth,
			pdfPage.GetClientSize().Height - currentY - PDFExportUtil._pageMargin));

		return result;
	}

	private static float DrawSummary(PdfDocument pdfDocument, PdfPage pdfPage, float currentY, SaleReturnOverviewModel saleReturn)
	{
		var summaryItems = new Dictionary<string, string>
		{
			["Total Products Returned: "] = saleReturn.TotalProducts.ToString(),
			["Total Quantity Returned: "] = saleReturn.TotalQuantity.ToString("N2")
		};

		// For sale returns, we don't typically have monetary values, so we pass 0 as the grand total
		return PDFExportUtil.DrawSummarySection(pdfDocument, pdfPage, currentY, summaryItems, 0, "Sale Return");
	}
}