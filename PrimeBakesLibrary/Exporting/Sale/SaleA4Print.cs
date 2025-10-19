using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Sale;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Order;
using PrimeBakesLibrary.Models.Product;
using PrimeBakesLibrary.Models.Sale;

using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;

namespace PrimeBakesLibrary.Exporting.Sale;

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

		float currentY = await PDFExportUtil.DrawCompanyInformation(pdfPage, "SALES INVOICE");

		// Add outlet details if it's not the primary location (LocationId != 1)
		if (sale.LocationId != 1)
			currentY = await DrawOutletDetailsAsync(pdfPage, currentY, location);

		currentY = await DrawInvoiceDetailsAsync(pdfPage, currentY, sale);
		var result = DrawItemDetails(pdfPage, currentY, saleProductCartModel);
		DrawSummary(pdfDocument, result.Page, result.Bounds.Bottom + 20, sale, saleDetails);

		return PDFExportUtil.FinalizePdfDocument(pdfDocument);
	}

	/// <summary>
	/// Draws outlet details section for non-primary locations
	/// </summary>
	private static async Task<float> DrawOutletDetailsAsync(PdfPage pdfPage, float currentY, LocationModel location)
	{
		// Calculate height based on content - basic height plus additional lines if needed
		var baseHeight = 60f; // Base height for name, GST, and contact
		var additionalHeight = 0f;

		var outlet = await LedgerData.LoadLedgerByLocation(location.Id);

		// Add height for address if it exists and is long
		if (!string.IsNullOrEmpty(outlet.Address))
		{
			// Estimate additional height needed for address wrapping
			var addressLength = outlet.Address.Length;
			if (addressLength > 80) // If address is long, add more height
				additionalHeight += 15f;
		}

		var totalHeight = baseHeight + additionalHeight;
		var detailsRect = new RectangleF(15, currentY, pdfPage.GetClientSize().Width - 30, totalHeight);

		// Draw the outlet box with a different background color to distinguish from main company
		pdfPage.Graphics.DrawRectangle(new PdfPen(PDFExportUtil._accentColor, 1),
			new PdfSolidBrush(Color.FromArgb(240, 248, 255)), detailsRect); // Light blue background

		var leftX = 20f;
		var detailY = currentY + 8f;

		// Section title
		pdfPage.Graphics.DrawString("Outlet Details", PDFExportUtil._subHeaderFont,
			new PdfSolidBrush(PDFExportUtil._accentColor), new PointF(leftX, detailY));

		detailY += 20f;

		// Outlet name
		pdfPage.Graphics.DrawString($"Outlet: {location.Name}", PDFExportUtil._normalFont,
			new PdfSolidBrush(PDFExportUtil._darkGray), new PointF(leftX, detailY));

		// Alias (if available)
		if (!string.IsNullOrEmpty(outlet.Alias))
		{
			detailY += 12f;
			pdfPage.Graphics.DrawString(outlet.Alias, PDFExportUtil._normalFont,
				new PdfSolidBrush(PDFExportUtil._darkGray), new PointF(leftX, detailY));
		}

		// GST Number (if available)
		if (!string.IsNullOrEmpty(outlet.GSTNo))
		{
			detailY += 12f;
			pdfPage.Graphics.DrawString($"GST NO: {outlet.GSTNo}", PDFExportUtil._normalFont,
				new PdfSolidBrush(PDFExportUtil._darkGray), new PointF(leftX, detailY));
		}

		// Address (if available)
		if (!string.IsNullOrEmpty(outlet.Address))
		{
			// Handle long addresses with text wrapping
			var availableWidth = detailsRect.Width - 20f; // Leave some margin
			var addressRect = new RectangleF(leftX, detailY, availableWidth, 30f);

			// Use a simple approach for address display
			if (outlet.Address.Length > 80)
			{
				detailY += 12f;

				// For very long addresses, try to break at a reasonable point
				var breakPoint = outlet.Address.LastIndexOf(' ', 80);
				if (breakPoint > 0)
				{
					var firstLine = outlet.Address.Substring(0, breakPoint);
					var secondLine = outlet.Address.Substring(breakPoint + 1);

					pdfPage.Graphics.DrawString($"Address: {firstLine}", PDFExportUtil._normalFont,
						new PdfSolidBrush(PDFExportUtil._darkGray), new PointF(leftX, detailY));
					detailY += 12f;
					pdfPage.Graphics.DrawString(secondLine, PDFExportUtil._normalFont,
						new PdfSolidBrush(PDFExportUtil._darkGray), new PointF(leftX + 60, detailY));
				}
				else
				{
					pdfPage.Graphics.DrawString($"Address: {outlet.Address}", PDFExportUtil._normalFont,
						new PdfSolidBrush(PDFExportUtil._darkGray), addressRect);
				}
			}
			else
			{
				pdfPage.Graphics.DrawString($"Address: {outlet.Address}", PDFExportUtil._normalFont,
					new PdfSolidBrush(PDFExportUtil._darkGray), new PointF(leftX, detailY));
			}
		}

		// Contact information (if available)
		if (!string.IsNullOrEmpty(outlet.Phone))
		{
			detailY += 12f;
			pdfPage.Graphics.DrawString($"Contact: {outlet.Phone}", PDFExportUtil._normalFont,
				new PdfSolidBrush(PDFExportUtil._darkGray), new PointF(leftX, detailY));
		}

		// Email information (if available)
		if (!string.IsNullOrEmpty(outlet.Email))
		{
			detailY += 12f;
			pdfPage.Graphics.DrawString($"Email: {outlet.Email}", PDFExportUtil._normalFont,
				new PdfSolidBrush(PDFExportUtil._darkGray), new PointF(leftX, detailY));
		}

		return detailsRect.Y + detailsRect.Height + 15f; // Add some spacing after the outlet box
	}

	private static async Task<float> DrawInvoiceDetailsAsync(PdfPage pdfPage, float currentY, SaleOverviewModel sale)
	{
		var leftColumnDetails = new Dictionary<string, string>
		{
			["Invoice No"] = sale.BillNo ?? "N/A",
			["Date"] = sale.SaleDateTime.ToString("dddd, MMMM dd, yyyy hh:mm tt"),
			["User"] = sale.UserName ?? "N/A"
		};

		if (sale.OrderId is not null)
		{
			leftColumnDetails["Order No"] = sale.OrderNo ?? "N/A";
			var order = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, sale.OrderId.Value);
			leftColumnDetails["Order Date"] = order.OrderDateTime.ToString("dddd, MMMM dd, yyyy hh:mm tt");
		}

		var rightColumnDetails = new Dictionary<string, string>();

		if (sale.CustomerId is not null)
		{
			rightColumnDetails["Cust. Name"] = sale.CustomerName ?? "N/A";
			rightColumnDetails["Cust. No."] = sale.CustomerNumber ?? "N/A";
		}

		if (sale.PartyId is not null)
			rightColumnDetails["Party"] = sale.PartyName ?? "N/A";

		if (!string.IsNullOrEmpty(sale.Remarks))
			leftColumnDetails["Remarks"] = sale.Remarks;

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

	private static float DrawSummary(PdfDocument pdfDocument, PdfPage pdfPage, float currentY, SaleOverviewModel sale, List<SaleDetailModel> saleDetails)
	{
		var summaryItems = new Dictionary<string, string>
		{
			["Total Products: "] = sale.TotalProducts.ToString(),
			["Total Quantity: "] = ((int)sale.TotalQuantity).ToString(),
			["Sub Total: "] = sale.BaseTotal.FormatIndianCurrency()
		};

		// Check if tax rates are consistent across all items
		bool uniformTaxRates = saleDetails.All(x =>
			x.CGSTPercent == sale.CGSTPercent &&
			x.SGSTPercent == sale.SGSTPercent &&
			x.IGSTPercent == sale.IGSTPercent);

		if (sale.ProductDiscountAmount > 0)
			summaryItems["Product Discount: "] = $"-{sale.ProductDiscountAmount.FormatIndianCurrency()}";

		if (sale.CGSTPercent > 0)
		{
			if (uniformTaxRates)
				summaryItems[$"CGST ({sale.CGSTPercent:N1}%):"] = sale.CGSTAmount.FormatIndianCurrency();
			else
				summaryItems[$"CGST:"] = sale.CGSTAmount.FormatIndianCurrency();
		}

		if (sale.SGSTPercent > 0)
		{
			if (uniformTaxRates)
				summaryItems[$"SGST ({sale.SGSTPercent:N1}%):"] = sale.SGSTAmount.FormatIndianCurrency();
			else
				summaryItems[$"SGST:"] = sale.SGSTAmount.FormatIndianCurrency();
		}

		if (sale.IGSTPercent > 0)
		{
			if (uniformTaxRates)
				summaryItems[$"IGST ({sale.IGSTPercent:N1}%):"] = sale.IGSTAmount.FormatIndianCurrency();
			else
				summaryItems[$"IGST:"] = sale.IGSTAmount.FormatIndianCurrency();
		}

		if (sale.BillDiscountAmount > 0)
			summaryItems[$"Bill Discount ({sale.BillDiscountPercent}%): "] = $"-{sale.BillDiscountAmount.FormatIndianCurrency()}";

		if (sale.RoundOff != 0)
			summaryItems["Round Off:"] = sale.RoundOff.FormatIndianCurrency();

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