using PrimeOrdersLibrary.Exporting;
using PrimeOrdersLibrary.Models.Sale;

namespace PrimeOrdersLibrary.Exporting.Sale;

public static class SaleExcelExport
{
	public static MemoryStream ExportSaleOverviewExcel(List<SaleOverviewModel> saleOverview, DateOnly _startDate, DateOnly _endDate)
	{
		// Create summary items dictionary
		Dictionary<string, object> summaryItems = new()
		{
			{ "Total Sales", saleOverview.Sum(s => s.Total) },
			{ "Total Transactions", saleOverview.Count },
			{ "Total Products", saleOverview.Sum(s => s.TotalProducts) },
			{ "Total Quantity", saleOverview.Sum(s => s.TotalQuantity) },
			{ "Discount Amount", saleOverview.Sum(s => s.DiscountAmount) },
			{ "Cash", saleOverview.Sum(s => s.Cash) },
			{ "Card", saleOverview.Sum(s => s.Card) },
			{ "UPI", saleOverview.Sum(s => s.UPI) },
			{ "Credit", saleOverview.Sum(s => s.Credit) }
		};

		// Define the column order for better readability
		List<string> columnOrder = [
					nameof(SaleOverviewModel.SaleId),
					nameof(SaleOverviewModel.SaleDateTime),
					nameof(SaleOverviewModel.LocationName),
					nameof(SaleOverviewModel.UserName),
					nameof(SaleOverviewModel.TotalProducts),
					nameof(SaleOverviewModel.TotalQuantity),
					nameof(SaleOverviewModel.BaseTotal),
					nameof(SaleOverviewModel.DiscountAmount),
					nameof(SaleOverviewModel.TotalTaxAmount),
					nameof(SaleOverviewModel.Total),
					nameof(SaleOverviewModel.Cash),
					nameof(SaleOverviewModel.Card),
					nameof(SaleOverviewModel.UPI),
					nameof(SaleOverviewModel.Credit),
					nameof(SaleOverviewModel.PartyName)
			];

		// Create a customized column settings for the report
		Dictionary<string, ExcelExportUtil.ColumnSetting> columnSettings = null;

		// Generate the Excel file
		string reportTitle = "Detailed Sales Report";
		string worksheetName = "Sales Details";

		return ExcelExportUtil.ExportToExcel(
			saleOverview,
			reportTitle,
			worksheetName,
			_startDate,
			_endDate,
			summaryItems,
			columnSettings,
			columnOrder);
	}
}
