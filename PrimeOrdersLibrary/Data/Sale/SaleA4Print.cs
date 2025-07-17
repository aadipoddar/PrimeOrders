using PrimeOrdersLibrary.Data.Common;
using PrimeOrdersLibrary.Exporting;
using PrimeOrdersLibrary.Models.Product;
using PrimeOrdersLibrary.Models.Sale;

using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;

namespace PrimeOrdersLibrary.Data.Sale;

public static class SaleA4Print
{
	public static async Task<MemoryStream> GenerateA4SaleBill(int saleId)
	{
		var sale = await SaleData.LoadSaleOverviewBySaleId(saleId);
		var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, sale.LocationId);
		var saleDetails = await SaleData.LoadSaleDetailBySale(saleId);

		List<SaleProductCartModel> saleProductCartModel = [];
		foreach (var item in saleDetails)
			saleProductCartModel.Add(new()
			{
				ProductId = item.ProductId,
				ProductName = (await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, item.ProductId)).Name,
				Quantity = item.Quantity,
				AfterDiscount = item.AfterDiscount,
				Rate = item.Rate,
				BaseTotal = item.BaseTotal,
				DiscPercent = item.DiscPercent,
				DiscAmount = item.DiscAmount,
				CGSTPercent = item.CGSTPercent,
				CGSTAmount = item.CGSTAmount,
				SGSTPercent = item.SGSTPercent,
				SGSTAmount = item.SGSTAmount,
				IGSTPercent = item.IGSTPercent,
				IGSTAmount = item.IGSTAmount,
				Total = item.Total
			});

		var (pdfDocument, pdfPage) = PDFExportUtil.CreateA4Document(location.Name);

		float currentY = PDFExportUtil.DrawCompanyInformation(pdfPage, "SALES INVOICE");
		currentY = DrawInvoiceDetails(pdfPage, currentY, sale);
		var result = DrawItemDetails(pdfPage, currentY, saleProductCartModel);
		DrawSummary(pdfDocument, result.Page, result.Bounds.Bottom + 20, sale);

		return PDFExportUtil.FinalizePdfDocument(pdfDocument);
	}

	private static float DrawInvoiceDetails(PdfPage pdfPage, float currentY, SaleOverviewModel sale)
	{
		var leftColumnDetails = new Dictionary<string, string>
		{
			["Invoice No"] = sale.BillNo ?? "N/A",
			["Date"] = sale.SaleDateTime.ToString("dddd, MMMM dd, yyyy hh:mm tt"),
			["User"] = sale.UserName ?? "N/A"
		};

		var rightColumnDetails = new Dictionary<string, string>();

		if (sale.OrderId is not null)
			rightColumnDetails["Order No"] = sale.OrderNo ?? "N/A";

		if (sale.PartyId is not null)
			rightColumnDetails["Party"] = sale.PartyName ?? "N/A";

		if (!string.IsNullOrEmpty(sale.Remarks))
			rightColumnDetails["Remarks"] = sale.Remarks;

		return PDFExportUtil.DrawInvoiceDetailsSection(pdfPage, currentY, "Invoice Details", leftColumnDetails, rightColumnDetails);
	}

	private static PdfGridLayoutResult DrawItemDetails(PdfPage pdfPage, float currentY, List<SaleProductCartModel> saleDetails)
	{
		var dataSource = saleDetails.Select((item, index) => new
		{
			SNo = index + 1,
			Name = item.ProductName.ToString(),
			Qty = (int)item.Quantity,
			Rate = (int)item.Rate,
			Amount = (int)item.BaseTotal,
			Total = (int)item.Total
		}).ToList();

		var tableWidth = pdfPage.GetClientSize().Width - PDFExportUtil._pageMargin * 2;
		var columnWidths = new float[]
		{
			tableWidth * 0.08f, // S.No
			tableWidth * 0.40f, // Name
			tableWidth * 0.12f, // Qty
			tableWidth * 0.15f, // Rate
			tableWidth * 0.12f, // Amount
			tableWidth * 0.13f  // Total
		};

		var columnAlignments = new PdfTextAlignment[]
		{
			PdfTextAlignment.Center, // S.No
			PdfTextAlignment.Left,   // Name
			PdfTextAlignment.Center, // Qty
			PdfTextAlignment.Right,  // Rate
			PdfTextAlignment.Right,  // Amount
			PdfTextAlignment.Right   // Total
		};

		var pdfGrid = PDFExportUtil.CreateStyledGrid(dataSource, columnWidths, columnAlignments);

		var result = pdfGrid.Draw(pdfPage, new RectangleF(PDFExportUtil._pageMargin, currentY, tableWidth,
			pdfPage.GetClientSize().Height - currentY - PDFExportUtil._pageMargin));

		return result;
	}

	private static float DrawSummary(PdfDocument pdfDocument, PdfPage pdfPage, float currentY, SaleOverviewModel sale)
	{
		var summaryItems = new Dictionary<string, string>
		{
			["Total Products: "] = sale.TotalProducts.ToString(),
			["Total Quantity: "] = ((int)sale.TotalQuantity).ToString(),
			["Sub Total: "] = sale.BaseTotal.FormatIndianCurrency()
		};

		if (sale.DiscountAmount > 0)
			summaryItems[$"Discount ({sale.DiscountPercent:N1}%):"] = $"-{sale.DiscountAmount.FormatIndianCurrency()}";

		if (sale.CGSTPercent > 0)
			summaryItems[$"CGST ({sale.CGSTPercent:N1}%):"] = sale.CGSTAmount.FormatIndianCurrency();

		if (sale.SGSTPercent > 0)
			summaryItems[$"SGST ({sale.SGSTPercent:N1}%):"] = sale.SGSTAmount.FormatIndianCurrency();

		if (sale.IGSTPercent > 0)
			summaryItems[$"IGST ({sale.IGSTPercent:N1}%):"] = sale.IGSTAmount.FormatIndianCurrency();

		var paymentMode = GetPaymentMode(sale);

		return PDFExportUtil.DrawSummarySection(pdfDocument, pdfPage, currentY, summaryItems, sale.Total, paymentMode);
	}

	private static string GetPaymentMode(SaleOverviewModel sale)
	{
		if (sale.Cash > 0 && sale.Cash >= sale.Card + sale.UPI + sale.Credit)
			return "Cash";
		else if (sale.Card > 0 && sale.Card >= sale.Cash + sale.UPI + sale.Credit)
			return "Card";
		else if (sale.UPI > 0 && sale.UPI >= sale.Cash + sale.Card + sale.Credit)
			return "UPI";
		else if (sale.Credit > 0 && sale.Credit >= sale.Cash + sale.Card + sale.UPI)
			return "Credit";
		else if (sale.Cash > 0 || sale.Card > 0 || sale.UPI > 0 || sale.Credit > 0)
			return "Multiple";
		else
			return "Unknown";
	}
}