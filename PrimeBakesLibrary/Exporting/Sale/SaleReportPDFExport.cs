using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Sale;

using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;

namespace PrimeBakesLibrary.Exporting.Sale;

public static class SaleReportPDFExport
{
	public static async Task<MemoryStream> GenerateA4SaleReport(List<SaleOverviewModel> saleOverview, DateOnly _startDate, DateOnly _endDate, LedgerModel? selectedSupplier)
	{
		var (pdfDocument, pdfPage) = PDFExportUtil.CreateA4Document();

		float currentY = await PDFExportUtil.DrawCompanyInformation(pdfPage, "SALES REGISTER");
		currentY = DrawInvoiceDetailsAsync(pdfPage, currentY, _startDate, _endDate, selectedSupplier);
		var result = DrawItemDetails(pdfPage, currentY, saleOverview);
		DrawSummary(pdfDocument, result.Page, result.Bounds.Bottom + 20, saleOverview);

		return PDFExportUtil.FinalizePdfDocument(pdfDocument);
	}
	private static float DrawInvoiceDetailsAsync(PdfPage pdfPage, float currentY, DateOnly startDate, DateOnly endDate, LedgerModel selectedSupplier)
	{
		var leftColumnDetails = new Dictionary<string, string>
		{
			["Date"] = $"{startDate:dd/MM/yy} to {endDate:dd/MM/yy}",
			["Party"] = selectedSupplier?.Name ?? "All",
		};

		return PDFExportUtil.DrawInvoiceDetailsSection(pdfPage, currentY, "Register Details", leftColumnDetails, []);
	}

	private static PdfGridLayoutResult DrawItemDetails(PdfPage pdfPage, float currentY, List<SaleOverviewModel> saleOverview)
	{
		var dataSource = saleOverview.Select((item, index) => new
		{
			TransType = item.SaleId < 0 ? "Return" : "Sale",
			item.BillNo,
			BillDate = item.SaleDateTime.ToString("dd-MM-yyyy"),
			Quantity = item.TotalQuantity.ToString("N2"),
			Discount = $"{item.BillDiscountPercent}% ({item.BillDiscountAmount.FormatIndianCurrency()})",
			Total = item.Total.FormatIndianCurrency()
		}).ToList();

		var tableWidth = pdfPage.GetClientSize().Width - PDFExportUtil._pageMargin * 2;
		var columnWidths = new float[]
		{
			tableWidth * 0.15f, // Type
			tableWidth * 0.20f, // Bill No
			tableWidth * 0.15f, // Bill Date
			tableWidth * 0.15f, // Qty
			tableWidth * 0.20f, // Discount
			tableWidth * 0.15f  // Total
		};

		var columnAlignments = new PdfTextAlignment[]
		{
			PdfTextAlignment.Left,   // Type
			PdfTextAlignment.Left,   // Bill No
			PdfTextAlignment.Center, // Bill Date
			PdfTextAlignment.Right,  // Qty
			PdfTextAlignment.Right,  // Discount
			PdfTextAlignment.Right   // Total
		};

		var pdfGrid = PDFExportUtil.CreateStyledGrid(dataSource, columnWidths, columnAlignments);

		pdfGrid.Headers[0].Cells[0].Value = "Trans Type";
		pdfGrid.Headers[0].Cells[1].Value = "Bill No";
		pdfGrid.Headers[0].Cells[2].Value = "Bill Date";
		pdfGrid.Headers[0].Cells[3].Value = "Quantity";
		pdfGrid.Headers[0].Cells[4].Value = "Discount";
		pdfGrid.Headers[0].Cells[5].Value = "Total Amt";

		var result = pdfGrid.Draw(pdfPage, new RectangleF(PDFExportUtil._pageMargin, currentY, tableWidth,
			pdfPage.GetClientSize().Height - currentY - PDFExportUtil._pageMargin));

		return result;
	}

	private static float DrawSummary(PdfDocument pdfDocument, PdfPage pdfPage, float currentY, List<SaleOverviewModel> saleOverview)
	{
		var summaryItems = new Dictionary<string, string>
		{
			["Total Sales: "] = saleOverview.Count(x => x.SaleId > 0).ToString(),
			["Total Returns: "] = saleOverview.Count(x => x.SaleId < 0).ToString(),
			[""] = "",
			["Total Sale Amt: "] = saleOverview.Sum(x => x.SaleId > 0 ? x.Total : 0).FormatIndianCurrency(),
			["Total Sale Discount: "] = $"-{saleOverview.Where(x => x.SaleId > 0).Sum(x => x.BillDiscountAmount).FormatIndianCurrency()}",
			["Total Return Amt: "] = saleOverview.Sum(x => x.SaleId < 0 ? x.Total : 0).FormatIndianCurrency()
		};

		return PDFExportUtil.DrawSummarySection(pdfDocument, pdfPage, currentY, summaryItems, saleOverview.Sum(x => x.Total));
	}
}
