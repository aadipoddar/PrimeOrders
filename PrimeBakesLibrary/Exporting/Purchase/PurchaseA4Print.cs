using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory;
using PrimeBakesLibrary.Models.Inventory;

using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;

namespace PrimeBakesLibrary.Exporting.Purchase;

public static class PurchaseA4Print
{
	public static async Task<MemoryStream> GenerateA4PurchaseBill(int purchaseId)
	{
		var purchase = await PurchaseData.LoadPurchaseOverviewByPurchaseId(purchaseId);
		var purchaseDetails = await PurchaseData.LoadPurchaseDetailByPurchase(purchaseId);

		List<PurchaseRawMaterialCartModel> purchaseDetailCartModel = [];
		foreach (var item in purchaseDetails)
			purchaseDetailCartModel.Add(new()
			{
				RawMaterialId = item.RawMaterialId,
				RawMaterialName = (await CommonData.LoadTableDataById<RawMaterialModel>(TableNames.RawMaterial, item.RawMaterialId)).Name,
				Quantity = item.Quantity,
				MeasurementUnit = item.MeasurementUnit,
				Rate = item.Rate,
				BaseTotal = item.BaseTotal,
				DiscPercent = item.DiscPercent,
				DiscAmount = item.DiscAmount,
				AfterDiscount = item.AfterDiscount,
				CGSTPercent = item.CGSTPercent,
				CGSTAmount = item.CGSTAmount,
				SGSTPercent = item.SGSTPercent,
				SGSTAmount = item.SGSTAmount,
				IGSTPercent = item.IGSTPercent,
				IGSTAmount = item.IGSTAmount,
				Total = item.Total,
				NetRate = item.NetRate
			});

		var (pdfDocument, pdfPage) = PDFExportUtil.CreateA4Document($"Purchase from {purchase.SupplierName}");

		float currentY = PDFExportUtil.DrawCompanyInformation(pdfPage, "PURCHASE INVOICE");
		currentY = DrawInvoiceDetails(pdfPage, currentY, purchase);
		var result = DrawItemDetails(pdfPage, currentY, purchaseDetailCartModel);
		DrawSummary(pdfDocument, result.Page, result.Bounds.Bottom + 20, purchase);

		return PDFExportUtil.FinalizePdfDocument(pdfDocument);
	}

	private static float DrawInvoiceDetails(PdfPage pdfPage, float currentY, PurchaseOverviewModel purchase)
	{
		var leftColumnDetails = new Dictionary<string, string>
		{
			["Invoice No"] = purchase.BillNo ?? "N/A",
			["Date"] = purchase.BillDateTime.ToString("dddd, MMMM dd, yyyy hh:mm tt"),
			["User"] = purchase.UserName ?? "N/A"
		};

		var rightColumnDetails = new Dictionary<string, string>
		{
			["Supplier"] = purchase.SupplierName ?? "N/A"
		};

		if (purchase.CashDiscountPercent > 0)
			rightColumnDetails[$"Cash Discount ({purchase.CashDiscountPercent:N1}%)"] = purchase.CashDiscountAmount.FormatIndianCurrency();

		if (!string.IsNullOrEmpty(purchase.Remarks))
			rightColumnDetails["Remarks"] = purchase.Remarks;

		return PDFExportUtil.DrawInvoiceDetailsSection(pdfPage, currentY, "Purchase Details", leftColumnDetails, rightColumnDetails);
	}

	private static PdfGridLayoutResult DrawItemDetails(PdfPage pdfPage, float currentY, List<PurchaseRawMaterialCartModel> purchaseDetails)
	{
		var dataSource = purchaseDetails.Select((item, index) => new
		{
			SNo = index + 1,
			Name = item.RawMaterialName.ToString(),
			Qty = item.Quantity.ToString("N2"),
			Unit = item.MeasurementUnit,
			Rate = (int)item.Rate,
			Amount = (int)item.BaseTotal,
			Total = (int)item.Total
		}).ToList();

		var tableWidth = pdfPage.GetClientSize().Width - PDFExportUtil._pageMargin * 2;
		var columnWidths = new float[]
		{
			tableWidth * 0.08f, // S.No
			tableWidth * 0.35f, // Name
			tableWidth * 0.12f, // Qty
			tableWidth * 0.08f, // Unit
			tableWidth * 0.15f, // Rate
			tableWidth * 0.11f, // Amount
			tableWidth * 0.11f  // Total
		};

		var columnAlignments = new PdfTextAlignment[]
		{
			PdfTextAlignment.Center, // S.No
			PdfTextAlignment.Left,   // Name
			PdfTextAlignment.Center, // Qty
			PdfTextAlignment.Center, // Unit
			PdfTextAlignment.Right,  // Rate
			PdfTextAlignment.Right,  // Amount
			PdfTextAlignment.Right   // Total
		};

		var pdfGrid = PDFExportUtil.CreateStyledGrid(dataSource, columnWidths, columnAlignments);

		var result = pdfGrid.Draw(pdfPage, new RectangleF(PDFExportUtil._pageMargin, currentY, tableWidth,
			pdfPage.GetClientSize().Height - currentY - PDFExportUtil._pageMargin));

		return result;
	}

	private static float DrawSummary(PdfDocument pdfDocument, PdfPage pdfPage, float currentY, PurchaseOverviewModel purchase)
	{
		var summaryItems = new Dictionary<string, string>
		{
			["Total Items: "] = purchase.TotalItems.ToString(),
			["Total Quantity: "] = purchase.TotalQuantity.ToString("N2"),
			["Base Total: "] = purchase.BaseTotal.FormatIndianCurrency()
		};

		if (purchase.DiscountAmount > 0)
			summaryItems["Discount:"] = $"-{purchase.DiscountAmount.FormatIndianCurrency()}";

		summaryItems["Sub Total: "] = purchase.SubTotal.FormatIndianCurrency();

		if (purchase.CGSTPercent > 0)
			summaryItems[$"CGST ({purchase.CGSTPercent:N1}%):"] = purchase.CGSTAmount.FormatIndianCurrency();

		if (purchase.SGSTPercent > 0)
			summaryItems[$"SGST ({purchase.SGSTPercent:N1}%):"] = purchase.SGSTAmount.FormatIndianCurrency();

		if (purchase.IGSTPercent > 0)
			summaryItems[$"IGST ({purchase.IGSTPercent:N1}%):"] = purchase.IGSTAmount.FormatIndianCurrency();

		if (purchase.CashDiscountAmount > 0)
			summaryItems[$"Cash Discount ({purchase.CashDiscountPercent:N1}%):"] = $"-{purchase.CashDiscountAmount.FormatIndianCurrency()}";

		return PDFExportUtil.DrawSummarySection(pdfDocument, pdfPage, currentY, summaryItems, purchase.Total);
	}
}