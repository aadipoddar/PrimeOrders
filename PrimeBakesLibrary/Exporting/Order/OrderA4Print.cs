using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Order;
using PrimeBakesLibrary.Data.Product;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Order;

using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;

namespace PrimeBakesLibrary.Exporting.Order;

public static class OrderA4Print
{
	public static async Task<MemoryStream> GenerateA4OrderDocument(int orderId)
	{
		// Load order basic information
		var order = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, orderId);
		var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, order.LocationId);
		var user = await CommonData.LoadTableDataById<UserModel>(TableNames.User, order.UserId);
		var orderDetails = await OrderData.LoadOrderDetailByOrder(orderId);

		// Create order product cart model for display
		List<OrderProductCartModel> orderProductCartModel = [];
		foreach (var item in orderDetails)
		{
			var product = (await ProductData.LoadProductRateByProduct(item.ProductId)).Where(p => p.LocationId == order.LocationId).FirstOrDefault();
			orderProductCartModel.Add(new()
			{
				ProductCategoryId = product.ProductCategoryId,
				ProductId = item.ProductId,
				ProductName = product.Name,
				Quantity = item.Quantity
			});
		}

		// Create PDF document
		var (pdfDocument, pdfPage) = PDFExportUtilOld.CreateA4Document();

		// Draw sections
		float currentY = await PDFExportUtilOld.DrawCompanyInformation(pdfPage, "ORDER");
		currentY = DrawOrderDetails(pdfPage, currentY, order, location, user);
		var result = DrawItemDetails(pdfPage, currentY, orderProductCartModel);
		DrawSummary(pdfDocument, result.Page, result.Bounds.Bottom + 20, orderProductCartModel, order);

		return PDFExportUtilOld.FinalizePdfDocument(pdfDocument);
	}

	private static float DrawOrderDetails(PdfPage pdfPage, float currentY, OrderModel order, LocationModel location, UserModel user)
	{
		var leftColumnDetails = new Dictionary<string, string>
		{
			["Order No"] = order.OrderNo ?? "N/A",
			["Date"] = order.OrderDateTime.ToString("dddd, MMMM dd, yyyy hh:mm tt"),
			["Created By"] = user.Name ?? "N/A"
		};

		var rightColumnDetails = new Dictionary<string, string>
		{
			["Location"] = location.Name ?? "N/A",
			["Status"] = order.SaleId.HasValue ? "Completed" : "Pending"
		};

		if (!string.IsNullOrEmpty(order.Remarks))
			rightColumnDetails["Remarks"] = order.Remarks;

		return PDFExportUtilOld.DrawInvoiceDetailsSection(pdfPage, currentY, "Order Details", leftColumnDetails, rightColumnDetails);
	}

	private static PdfGridLayoutResult DrawItemDetails(PdfPage pdfPage, float currentY, List<OrderProductCartModel> orderProducts)
	{
		var dataSource = orderProducts.Select((item, index) => new
		{
			SNo = index + 1,
			Name = item.ProductName.ToString(),
			Qty = item.Quantity.ToString("N2")
		}).ToList();

		var tableWidth = pdfPage.GetClientSize().Width - PDFExportUtilOld._pageMargin * 2;
		var columnWidths = new float[]
		{
			tableWidth * 0.15f, // S.No
			tableWidth * 0.60f, // Name
			tableWidth * 0.25f  // Qty
		};

		var columnAlignments = new PdfTextAlignment[]
		{
			PdfTextAlignment.Center, // S.No
			PdfTextAlignment.Left,   // Name
			PdfTextAlignment.Center  // Qty
		};

		var pdfGrid = PDFExportUtilOld.CreateStyledGrid(dataSource, columnWidths, columnAlignments);

		var result = pdfGrid.Draw(pdfPage, new RectangleF(PDFExportUtilOld._pageMargin, currentY, tableWidth,
			pdfPage.GetClientSize().Height - currentY - PDFExportUtilOld._pageMargin));

		return result;
	}

	private static float DrawSummary(PdfDocument pdfDocument, PdfPage pdfPage, float currentY, List<OrderProductCartModel> orderProducts, OrderModel order)
	{
		var summaryItems = new Dictionary<string, string>
		{
			["Total Products:"] = orderProducts.Count.ToString(),
			["Total Quantity:"] = orderProducts.Sum(p => p.Quantity).ToString("N2"),
			["Order Status:"] = order.SaleId.HasValue ? "Completed" : "Pending"
		};

		// Add completion info if order is completed
		if (order.SaleId.HasValue)
			summaryItems["Sale ID:"] = order.SaleId.Value.ToString();

		return PDFExportUtilOld.DrawSummarySection(pdfDocument, pdfPage, currentY, summaryItems, 0);
	}
}