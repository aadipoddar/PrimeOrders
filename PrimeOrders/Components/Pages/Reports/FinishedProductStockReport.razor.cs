using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;

namespace PrimeOrders.Components.Pages.Reports;

public partial class FinishedProductStockReport
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] private IJSRuntime JS { get; set; }

	private UserModel _user;
	private bool _isLoading = true;

	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now);
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);

	private List<LocationModel> _locations = [];
	private int _selectedLocationId;
	private List<ProductStockDetailModel> _stockDetails = [];

	private SfGrid<ProductStockDetailModel> _sfGrid;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_isLoading = true;

		if (!((_user = (await AuthService.ValidateUser(JS, NavManager)).User) is not null))
			return;

		await LoadLocations();
		await LoadStockDetails();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadLocations()
	{
		_selectedLocationId = _user.LocationId;

		if (_user.LocationId == 1)
			_locations = await CommonData.LoadTableData<LocationModel>(TableNames.Location);
	}

	private async Task LoadStockDetails() =>
		_stockDetails = await StockData.LoadProductStockDetailsByDateLocationId(
			_startDate.ToDateTime(new TimeOnly(0, 0)),
			_endDate.ToDateTime(new TimeOnly(23, 59)),
			_selectedLocationId);

	private async Task DateRangeChanged(RangePickerEventArgs<DateOnly> args)
	{
		_startDate = args.StartDate;
		_endDate = args.EndDate;
		await LoadStockDetails();
	}

	private async Task OnLocationChanged(ChangeEventArgs<int, LocationModel> args)
	{
		_selectedLocationId = args.Value;
		await LoadStockDetails();
	}

	private async Task ExportToPdf()
	{
		if (_sfGrid is not null)
			await _sfGrid.ExportToPdfAsync();
	}

	private async Task ExportToExcel()
	{
		if (_stockDetails is null || _stockDetails.Count == 0)
		{
			await JS.InvokeVoidAsync("alert", "No data to export");
			return;
		}

		// Create summary items dictionary with key stock metrics
		Dictionary<string, object> summaryItems = new()
		{
			{ "Total Stock Items", _stockDetails.Count },
			{ "Opening Stock", _stockDetails.Sum(s => s.OpeningStock) },
			{ "Total Production", _stockDetails.Sum(s => s.PurchaseStock) },
			{ "Total Sales", _stockDetails.Sum(s => s.SaleStock) },
			{ "Monthly Stock", _stockDetails.Sum(s => s.MonthlyStock) },
			{ "Closing Stock", _stockDetails.Sum(s => s.ClosingStock) },
			{ "Stock Movement", _stockDetails.Sum(s => s.PurchaseStock + s.SaleStock) },
			{ "Net Stock Change", _stockDetails.Sum(s => s.ClosingStock - s.OpeningStock) }
		};

		// Add top categories summary data
		var topCategories = _stockDetails
			.GroupBy(s => s.ProductCategoryName)
			.OrderByDescending(g => g.Sum(s => s.ClosingStock))
			.Take(3)
			.ToList();

		foreach (var category in topCategories)
			summaryItems.Add($"Category: {category.Key}", category.Sum(s => s.ClosingStock));

		// Define the column order for better readability
		List<string> columnOrder = [
			nameof(ProductStockDetailModel.ProductCode),
			nameof(ProductStockDetailModel.ProductName),
			nameof(ProductStockDetailModel.ProductCategoryName),
			nameof(ProductStockDetailModel.OpeningStock),
			nameof(ProductStockDetailModel.PurchaseStock),
			nameof(ProductStockDetailModel.SaleStock),
			nameof(ProductStockDetailModel.MonthlyStock),
			nameof(ProductStockDetailModel.ClosingStock)
		];

		// Define custom column settings
		var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
		{
			[nameof(ProductStockDetailModel.ProductCode)] = new()
			{
				DisplayName = "Product Code",
				Width = 12,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(ProductStockDetailModel.ProductName)] = new()
			{
				DisplayName = "Product Name",
				Width = 30,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(ProductStockDetailModel.ProductCategoryName)] = new()
			{
				DisplayName = "Category",
				Width = 20,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(ProductStockDetailModel.OpeningStock)] = new()
			{
				DisplayName = "Opening Stock",
				Format = "#,##0.00",
				Width = 15,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight,
				FormatCallback = (value) =>
				{
					if (value == null) return null;
					var stockValue = Convert.ToDecimal(value);
					return new ExcelExportUtil.FormatInfo
					{
						Bold = stockValue <= 0,
						FontColor = stockValue <= 0 ? Syncfusion.Drawing.Color.FromArgb(198, 40, 40) : null
					};
				}
			},
			[nameof(ProductStockDetailModel.PurchaseStock)] = new()
			{
				DisplayName = "Production",
				Format = "#,##0.00",
				Width = 15,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight,
				FormatCallback = (value) =>
				{
					if (value == null) return null;
					var stockValue = Convert.ToDecimal(value);
					return new ExcelExportUtil.FormatInfo
					{
						Bold = stockValue > 0,
						FontColor = stockValue > 0 ? Syncfusion.Drawing.Color.FromArgb(56, 142, 60) : null
					};
				}
			},
			[nameof(ProductStockDetailModel.SaleStock)] = new()
			{
				DisplayName = "Sales",
				Format = "#,##0.00",
				Width = 15,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight,
				FormatCallback = (value) =>
				{
					if (value == null) return null;
					var stockValue = Convert.ToDecimal(value);
					return new ExcelExportUtil.FormatInfo
					{
						Bold = stockValue > 0,
						FontColor = stockValue > 0 ? Syncfusion.Drawing.Color.FromArgb(239, 108, 0) : null
					};
				}
			},
			[nameof(ProductStockDetailModel.MonthlyStock)] = new()
			{
				DisplayName = "Monthly Stock",
				Format = "#,##0.00",
				Width = 15,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(ProductStockDetailModel.ClosingStock)] = new()
			{
				DisplayName = "Closing Stock",
				Format = "#,##0.00",
				Width = 15,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight,
				HighlightNegative = true,
				FormatCallback = (value) =>
				{
					if (value is null) return null;

					var stockValue = Convert.ToDecimal(value);
					if (stockValue <= 0)
					{
						return new ExcelExportUtil.FormatInfo
						{
							Bold = true,
							FontColor = Syncfusion.Drawing.Color.FromArgb(198, 40, 40)
						};
					}
					else if (stockValue <= 5)
					{
						return new ExcelExportUtil.FormatInfo
						{
							Bold = true,
							FontColor = Syncfusion.Drawing.Color.FromArgb(239, 108, 0)
						};
					}

					return null;
				}
			}
		};

		// Generate title based on location if selected
		string reportTitle = "Finished Product Stock Report";

		if (_selectedLocationId > 0 && _user.LocationId == 1)
		{
			var location = _locations.FirstOrDefault(l => l.Id == _selectedLocationId);
			if (location != null)
				reportTitle = $"Finished Product Stock Report - {location.Name}";
		}

		string worksheetName = "Product Stock Details";

		var memoryStream = ExcelExportUtil.ExportToExcel(
			_stockDetails,
			reportTitle,
			worksheetName,
			_startDate,
			_endDate,
			summaryItems,
			columnSettings,
			columnOrder);

		var fileName = $"Finished_Product_Stock_Report_{_startDate:yyyy-MM-dd}_to_{_endDate:yyyy-MM-dd}.xlsx";
		await JS.InvokeVoidAsync("saveAs", Convert.ToBase64String(memoryStream.ToArray()), fileName);
	}

	// Chart data methods
	private List<StockOverviewData> GetStockOverviewData()
	{
		var result = new List<StockOverviewData>
		{
			new() { Component = "Opening Stock", Value = _stockDetails.Sum(s => s.OpeningStock) },
			new() { Component = "Production", Value = _stockDetails.Sum(s => s.PurchaseStock) },
			new() { Component = "Sales", Value = _stockDetails.Sum(s => s.SaleStock) },
			new() { Component = "Monthly Stock", Value = _stockDetails.Sum(s => s.MonthlyStock) },
			new() { Component = "Closing Stock", Value = _stockDetails.Sum(s => s.ClosingStock) }
		};

		return result;
	}

	private List<CategoryDistributionData> GetCategoryDistributionData()
	{
		var result = _stockDetails
			.GroupBy(s => s.ProductCategoryName)
			.Select(group => new CategoryDistributionData
			{
				CategoryName = group.Key,
				StockCount = group.Sum(s => s.ClosingStock)
			})
			.OrderByDescending(c => c.StockCount)
			.Take(10)
			.ToList();

		return result;
	}

	private List<TopMovingItemsData> GetTopMovingItemsData()
	{
		var result = _stockDetails
			.Select(s => new TopMovingItemsData
			{
				ItemName = s.ProductName,
				Movement = s.PurchaseStock + s.SaleStock
			})
			.OrderByDescending(i => i.Movement)
			.Take(10)
			.ToList();

		return result;
	}

	private List<OpeningClosingData> GetOpeningClosingData()
	{
		var result = _stockDetails
			.Select(s => new OpeningClosingData
			{
				ItemName = s.ProductName,
				OpeningStock = s.OpeningStock,
				ClosingStock = s.ClosingStock
			})
			.OrderByDescending(i => Math.Abs(i.ClosingStock - i.OpeningStock))
			.Take(10)
			.ToList();

		return result;
	}

	// Chart data classes
	public class StockOverviewData
	{
		public string Component { get; set; }
		public decimal Value { get; set; }
	}

	public class CategoryDistributionData
	{
		public string CategoryName { get; set; }
		public decimal StockCount { get; set; }
	}

	public class TopMovingItemsData
	{
		public string ItemName { get; set; }
		public decimal Movement { get; set; }
	}

	public class OpeningClosingData
	{
		public string ItemName { get; set; }
		public decimal OpeningStock { get; set; }
		public decimal ClosingStock { get; set; }
	}
}