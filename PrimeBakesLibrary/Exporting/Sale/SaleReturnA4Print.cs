using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Sale;
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
		var saleReturn = await SaleReturnData.LoadSaleReturnOverviewBySaleReturnId(saleReturnId);
		var saleReturnDetails = await SaleReturnData.LoadSaleReturnDetailBySaleReturn(saleReturnId);

		List<SaleReturnProductCartModel> saleReturnProductCartModel = [];
		foreach (var item in saleReturnDetails)
			saleReturnProductCartModel.Add(new()
			{
				ProductId = item.ProductId,
				ProductName = (await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, item.ProductId)).Name,
				ProductCategoryId = (await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, item.ProductId)).ProductCategoryId,
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

		var (pdfDocument, pdfPage) = PDFExportUtil.CreateA4Document();

		float currentY = await PDFExportUtil.DrawCompanyInformation(pdfPage, "SALES RETURN INVOICE");

		currentY = DrawInvoiceDetailsAsync(pdfPage, currentY, saleReturn);
		var result = DrawItemDetails(pdfPage, currentY, saleReturnProductCartModel);
		DrawSummary(pdfDocument, result.Page, result.Bounds.Bottom + 20, saleReturn, saleReturnDetails);

		return PDFExportUtil.FinalizePdfDocument(pdfDocument);
	}

	private static float DrawInvoiceDetailsAsync(PdfPage pdfPage, float currentY, SaleReturnOverviewModel saleReturn)
	{
		var leftColumnDetails = new Dictionary<string, string>
		{
			["Invoice No"] = saleReturn.BillNo ?? "N/A",
			["Date"] = saleReturn.SaleReturnDateTime.ToString("dddd, MMMM dd, yyyy hh:mm tt"),
			["User"] = saleReturn.UserName ?? "N/A"
		};

		var rightColumnDetails = new Dictionary<string, string>();

		if (saleReturn.CustomerId is not null)
		{
			rightColumnDetails["Cust. Name"] = saleReturn.CustomerName ?? "N/A";
			rightColumnDetails["Cust. No."] = saleReturn.CustomerNumber ?? "N/A";
		}

		if (saleReturn.PartyId is not null)
			rightColumnDetails["Party"] = saleReturn.PartyName ?? "N/A";

		if (!string.IsNullOrEmpty(saleReturn.Remarks))
			leftColumnDetails["Remarks"] = saleReturn.Remarks;

		return PDFExportUtil.DrawInvoiceDetailsSection(pdfPage, currentY, "Invoice Details", leftColumnDetails, rightColumnDetails);
	}

	private static PdfGridLayoutResult DrawItemDetails(PdfPage pdfPage, float currentY, List<SaleReturnProductCartModel> saleDetails)
	{
		var dataSource = saleDetails.Select((item, index) => new
		{
			SNo = index + 1,
			Name = item.ProductName.ToString(),
			Qty = (int)item.Quantity,
			Rate = (int)item.Rate,
			Amount = (int)item.BaseTotal,
			Discount = $"{item.DiscPercent} ({(int)item.DiscAmount})",
			Total = (int)item.Total
		}).ToList();

		var tableWidth = pdfPage.GetClientSize().Width - PDFExportUtil._pageMargin * 2;
		var columnWidths = new float[]
		{
			tableWidth * 0.08f, // S.No
			tableWidth * 0.35f, // Name
			tableWidth * 0.08f, // Qty
			tableWidth * 0.12f, // Rate
			tableWidth * 0.12f, // Amount
			tableWidth * 0.12f, // Discount
			tableWidth * 0.13f  // Total
		};

		var columnAlignments = new PdfTextAlignment[]
		{
			PdfTextAlignment.Center, // S.No
			PdfTextAlignment.Left,   // Name
			PdfTextAlignment.Center, // Qty
			PdfTextAlignment.Right,  // Rate
			PdfTextAlignment.Right,  // Amount
			PdfTextAlignment.Right,  // Discount
			PdfTextAlignment.Right   // Total
		};

		var pdfGrid = PDFExportUtil.CreateStyledGrid(dataSource, columnWidths, columnAlignments);

		var result = pdfGrid.Draw(pdfPage, new RectangleF(PDFExportUtil._pageMargin, currentY, tableWidth,
			pdfPage.GetClientSize().Height - currentY - PDFExportUtil._pageMargin));

		return result;
	}

	private static float DrawSummary(PdfDocument pdfDocument, PdfPage pdfPage, float currentY, SaleReturnOverviewModel saleReturn, List<SaleReturnDetailModel> saleReturnDetails)
	{
		var summaryItems = new Dictionary<string, string>
		{
			["Total Products: "] = saleReturn.TotalProducts.ToString(),
			["Total Quantity: "] = ((int)saleReturn.TotalQuantity).ToString(),
			["Sub Total: "] = saleReturn.BaseTotal.FormatIndianCurrency()
		};

		bool uniformDiscount = saleReturnDetails.All(x => x.DiscPercent == saleReturn.DiscountPercent);

		// Check if tax rates are consistent across all items
		bool uniformTaxRates = saleReturnDetails.All(x =>
			x.CGSTPercent == saleReturn.CGSTPercent &&
			x.SGSTPercent == saleReturn.SGSTPercent &&
			x.IGSTPercent == saleReturn.IGSTPercent);

		if (saleReturn.DiscountAmount > 0)
		{
			if (uniformDiscount)
				summaryItems[$"Discount ({saleReturn.DiscountPercent:N1}%):"] = $"-{saleReturn.DiscountAmount.FormatIndianCurrency()}";
			else
				summaryItems[$"Discount:"] = $"-{saleReturn.DiscountAmount.FormatIndianCurrency()}";
		}

		if (saleReturn.CGSTPercent > 0)
		{
			if (uniformTaxRates)
				summaryItems[$"CGST ({saleReturn.CGSTPercent:N1}%):"] = saleReturn.CGSTAmount.FormatIndianCurrency();
			else
				summaryItems[$"CGST:"] = saleReturn.CGSTAmount.FormatIndianCurrency();
		}

		if (saleReturn.SGSTPercent > 0)
		{
			if (uniformTaxRates)
				summaryItems[$"SGST ({saleReturn.SGSTPercent:N1}%):"] = saleReturn.SGSTAmount.FormatIndianCurrency();
			else
				summaryItems[$"SGST:"] = saleReturn.SGSTAmount.FormatIndianCurrency();
		}

		if (saleReturn.IGSTPercent > 0)
		{
			if (uniformTaxRates)
				summaryItems[$"IGST ({saleReturn.IGSTPercent:N1}%):"] = saleReturn.IGSTAmount.FormatIndianCurrency();
			else
				summaryItems[$"IGST:"] = saleReturn.IGSTAmount.FormatIndianCurrency();
		}

		if (saleReturn.RoundOff != 0)
			summaryItems["Round Off:"] = saleReturn.RoundOff.FormatIndianCurrency();

		var paymentMode = GetPaymentMode(saleReturn);

		return PDFExportUtil.DrawSummarySection(pdfDocument, pdfPage, currentY, summaryItems, saleReturn.Total, paymentMode);
	}

	private static string GetPaymentMode(SaleReturnOverviewModel sale)
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