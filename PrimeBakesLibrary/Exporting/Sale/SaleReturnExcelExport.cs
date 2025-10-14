using PrimeBakesLibrary.Models.Sale;

namespace PrimeBakesLibrary.Exporting.Sale;

public static class SaleReturnExcelExport
{
	public static MemoryStream ExportSaleReturnOverviewExcel(
		List<SaleReturnOverviewModel> saleReturnOverviews,
		DateOnly startDate,
		DateOnly endDate)
	{
		// Create summary items dictionary with key metrics
		Dictionary<string, object> summaryItems = new()
		{
			{ "Total Returns", saleReturnOverviews.Sum(s => s.Total) },
			{ "Total Transactions", saleReturnOverviews.Count },
			{ "Total Products", saleReturnOverviews.Sum(s => s.TotalProducts) },
			{ "Total Quantity", saleReturnOverviews.Sum(s => s.TotalQuantity) },
			{ "Discount Amount", saleReturnOverviews.Sum(s => s.DiscountAmount) },
			{ "Cash", saleReturnOverviews.Sum(s => s.Cash) },
			{ "Card", saleReturnOverviews.Sum(s => s.Card) },
			{ "UPI", saleReturnOverviews.Sum(s => s.UPI) },
			{ "Credit", saleReturnOverviews.Sum(s => s.Credit) }
		};

		// Define the column order for better readability
		List<string> columnOrder = [
			nameof(SaleOverviewModel.SaleId),
					nameof(SaleReturnOverviewModel.BillNo),
					nameof(SaleReturnOverviewModel.SaleReturnDateTime),
					nameof(SaleReturnOverviewModel.LocationName),
					nameof(SaleReturnOverviewModel.UserName),
					nameof(SaleReturnOverviewModel.TotalProducts),
					nameof(SaleReturnOverviewModel.TotalQuantity),
					nameof(SaleReturnOverviewModel.BaseTotal),
					nameof(SaleReturnOverviewModel.DiscountPercent),
					nameof(SaleReturnOverviewModel.DiscountAmount),
					nameof(SaleReturnOverviewModel.DiscountReason),
					nameof(SaleReturnOverviewModel.SubTotal),
					nameof(SaleReturnOverviewModel.CGSTPercent),
					nameof(SaleReturnOverviewModel.CGSTAmount),
					nameof(SaleReturnOverviewModel.SGSTPercent),
					nameof(SaleReturnOverviewModel.SGSTAmount),
					nameof(SaleReturnOverviewModel.IGSTPercent),
					nameof(SaleReturnOverviewModel.IGSTAmount),
					nameof(SaleReturnOverviewModel.TotalTaxAmount),
					nameof(SaleReturnOverviewModel.RoundOff),
					nameof(SaleReturnOverviewModel.Total),
					nameof(SaleReturnOverviewModel.Cash),
					nameof(SaleReturnOverviewModel.Card),
					nameof(SaleReturnOverviewModel.UPI),
					nameof(SaleReturnOverviewModel.Credit),
					nameof(SaleReturnOverviewModel.Remarks),
					nameof(SaleReturnOverviewModel.PartyName),
					nameof(SaleReturnOverviewModel.CustomerName),
					nameof(SaleReturnOverviewModel.CustomerNumber),
					nameof(SaleReturnOverviewModel.CreatedAt)
		];

		// Create a customized column settings for the report
		Dictionary<string, ExcelExportUtil.ColumnSetting> columnSettings = null;

		// Generate title based on location selection if applicable
		string reportTitle = "Sale Return Report";
		string worksheetName = "Return Details";

		return ExcelExportUtil.ExportToExcel(
			saleReturnOverviews,
			reportTitle,
			worksheetName,
			startDate,
			endDate,
			summaryItems,
			columnSettings,
			columnOrder);
	}
}