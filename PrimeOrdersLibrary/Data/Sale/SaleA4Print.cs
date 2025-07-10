using PrimeOrdersLibrary.Data.Common;
using PrimeOrdersLibrary.Models.Product;
using PrimeOrdersLibrary.Models.Sale;

using Syncfusion.Pdf;

namespace PrimeOrdersLibrary.Data.Sale;

public static class SaleA4Print
{
	public static async Task<MemoryStream> GenerateA4SaleBill(int saleId)
	{
		var sale = await SaleData.LoadSaleOverviewBySaleId(saleId);

		using var pdfDocument = new PdfDocument();
		var pdfPage = pdfDocument.Pages.Add();

		pdfDocument.PageSettings.Size = PdfPageSize.A4;

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

		pdfDocument.Template.Top = PDFExportUtil.DrawHeader(pdfDocument, location.Name);
		pdfDocument.Template.Bottom = PDFExportUtil.DrawFooter(pdfDocument);

		float currentY = PDFExportUtil.DrawCompanyInformation(pdfPage, "SALES INVOICE");
		currentY = PDFExportUtil.DrawInvoiceDetails(pdfPage, currentY, sale);
		var result = PDFExportUtil.DrawItemDetails(pdfPage, currentY, saleProductCartModel);
		currentY = PDFExportUtil.DrawSummary(pdfDocument, result.Page, result.Bounds.Bottom + 20, sale);

		using var stream = new MemoryStream();
		pdfDocument.Save(stream);
		pdfDocument.Close();
		return stream;
	}
}
