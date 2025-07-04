using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.Grids;

namespace PrimeOrders.Components.Pages.Reports;

public partial class KitchenProductionReport
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	private bool _isLoading = true;
	private UserModel _user;

	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30));
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);
	private int _selectedKitchenId = 0;

	private List<KitchenProductionOverviewModel> _kitchenProductionOverviews = [];
	private List<KitchenModel> _kitchens = [];

	private SfGrid<KitchenProductionOverviewModel> _sfGrid;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_isLoading = true;

		if (!((_user = (await AuthService.ValidateUser(JS, NavManager, UserRoles.Inventory, primaryLocationRequirement: true)).User) is not null))
			return;

		await LoadKitchens();
		await LoadKitchenProductionData();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadKitchens()
	{
		_kitchens = await CommonData.LoadTableDataByStatus<KitchenModel>(TableNames.Kitchen);
		// Add "All Kitchens" option
		_kitchens.Insert(0, new KitchenModel { Id = 0, Name = "All Kitchens" });
	}

	private async Task LoadKitchenProductionData()
	{
		if (_selectedKitchenId > 0)
		{
			_kitchenProductionOverviews = await KitchenProductionData.LoadKitchenProductionDetailsByDate(
				_startDate.ToDateTime(new TimeOnly(0, 0)),
				_endDate.ToDateTime(new TimeOnly(23, 59)));

			_kitchenProductionOverviews = [.. _kitchenProductionOverviews.Where(i => i.KitchenId == _selectedKitchenId)];
		}
		else
		{
			_kitchenProductionOverviews = await KitchenProductionData.LoadKitchenProductionDetailsByDate(
				_startDate.ToDateTime(new TimeOnly(0, 0)),
				_endDate.ToDateTime(new TimeOnly(23, 59)));
		}
	}

	private async void DateRangeChanged(RangePickerEventArgs<DateOnly> args)
	{
		_startDate = args.StartDate;
		_endDate = args.EndDate;
		await RefreshData();
	}

	private async void OnKitchenChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<int, KitchenModel> args)
	{
		_selectedKitchenId = args.Value;
		await RefreshData();
	}

	private async Task RefreshData()
	{
		_isLoading = true;
		StateHasChanged();

		await LoadKitchenProductionData();

		_isLoading = false;
		StateHasChanged();
	}

	private void OnRowSelected(RowSelectEventArgs<KitchenProductionOverviewModel> args) =>
			NavManager.NavigateTo($"/Inventory/Kitchen-Production/{args.Data.KitchenProductionId}");

	// Chart data methods
	private List<KitchenWiseData> GetKitchenWiseData() =>
		[.. _kitchenProductionOverviews
			.GroupBy(i => i.KitchenName)
			.Select(g => new KitchenWiseData
			{
				KitchenName = g.Key,
				TotalQuantity = g.Sum(x => x.TotalQuantity)
			})
			.OrderByDescending(x => x.TotalQuantity)
			.Take(10)];

	private List<DailyProductionData> GetDailyProductionData() =>
		[.. _kitchenProductionOverviews
			.GroupBy(i => DateOnly.FromDateTime(i.ProductionDate))
			.Select(g => new DailyProductionData
			{
				Date = g.Key.ToString("dd/MM"),
				TotalQuantity = g.Sum(x => x.TotalQuantity)
			})
			.OrderBy(x => DateOnly.Parse(x.Date, new System.Globalization.CultureInfo("en-GB")))];

	private List<ProductCategoryData> GetProductCategoryData() =>
		[.. _kitchenProductionOverviews
			.GroupBy(i => i.KitchenName)
			.Select(g => new ProductCategoryData
			{
				CategoryName = g.Key,
				ProductCount = g.Count()
			})
			.OrderByDescending(x => x.ProductCount)
			.Take(10)];

	private async Task ExportToPdf() =>
		await _sfGrid.ExportToPdfAsync();

	private async Task ExportToExcel()
	{
		if (_kitchenProductionOverviews is null || _kitchenProductionOverviews.Count == 0)
		{
			await JS.InvokeVoidAsync("alert", "No data to export");
			return;
		}

		// Create summary items dictionary with key metrics
		Dictionary<string, object> summaryItems = new()
		{
			{ "Total Transactions", _kitchenProductionOverviews.Count },
			{ "Total Products", _kitchenProductionOverviews.Sum(_ => _.TotalProducts) },
			{ "Total Quantity", _kitchenProductionOverviews.Sum(_ => _.TotalQuantity) },
			{ "Kitchens Active", _kitchenProductionOverviews.Select(_ => _.KitchenId).Distinct().Count() }
		};

		// Add top kitchens summary data
		var topKitchens = _kitchenProductionOverviews
			.GroupBy(i => i.KitchenName)
			.OrderByDescending(g => g.Sum(x => x.TotalQuantity))
			.Take(3)
			.ToList();

		foreach (var kitchen in topKitchens)
			summaryItems.Add($"Kitchen: {kitchen.Key}", kitchen.Sum(k => k.TotalQuantity));

		// Define the column order for better readability
		List<string> columnOrder = [
			nameof(KitchenProductionOverviewModel.TransactionNo),
			nameof(KitchenProductionOverviewModel.ProductionDate),
			nameof(KitchenProductionOverviewModel.KitchenName),
			nameof(KitchenProductionOverviewModel.UserName),
			nameof(KitchenProductionOverviewModel.TotalProducts),
			nameof(KitchenProductionOverviewModel.TotalQuantity),
			nameof(KitchenProductionOverviewModel.Status)
		];

		// Define custom column settings
		var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
		{
			[nameof(KitchenProductionOverviewModel.TransactionNo)] = new()
			{
				DisplayName = "Transaction #",
				Width = 15,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(KitchenProductionOverviewModel.ProductionDate)] = new()
			{
				DisplayName = "Production Date",
				Format = "dd-MMM-yyyy",
				Width = 15,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(KitchenProductionOverviewModel.KitchenName)] = new()
			{
				DisplayName = "Kitchen",
				Width = 20,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(KitchenProductionOverviewModel.UserName)] = new()
			{
				DisplayName = "Produced By",
				Width = 18,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(KitchenProductionOverviewModel.TotalProducts)] = new()
			{
				DisplayName = "Products",
				Format = "#,##0",
				Width = 10,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(KitchenProductionOverviewModel.TotalQuantity)] = new()
			{
				DisplayName = "Total Quantity",
				Format = "#,##0.00",
				Width = 15,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight,
				FormatCallback = (value) =>
				{
					if (value == null) return null;
					var qtyValue = Convert.ToDecimal(value);
					return new ExcelExportUtil.FormatInfo
					{
						Bold = qtyValue > 10,
						FontColor = qtyValue > 10 ? Syncfusion.Drawing.Color.FromArgb(56, 142, 60) : null
					};
				}
			}
		};

		// Generate title based on kitchen selection if applicable
		string reportTitle = "Kitchen Production Report";

		if (_selectedKitchenId > 0)
		{
			var kitchen = _kitchens.FirstOrDefault(k => k.Id == _selectedKitchenId);
			if (kitchen != null)
				reportTitle = $"Kitchen Production Report - {kitchen.Name}";
		}

		string worksheetName = "Production Details";

		var memoryStream = ExcelExportUtil.ExportToExcel(
			_kitchenProductionOverviews,
			reportTitle,
			worksheetName,
			_startDate,
			_endDate,
			summaryItems,
			columnSettings,
			columnOrder);

		// Generate appropriate filename
		string filenameSuffix = string.Empty;
		if (_selectedKitchenId > 0)
		{
			var kitchen = _kitchens.FirstOrDefault(k => k.Id == _selectedKitchenId);
			if (kitchen is not null)
				filenameSuffix = $"_{kitchen.Name}";
		}

		var fileName = $"Kitchen_Production_Report{filenameSuffix}_{_startDate:yyyy-MM-dd}_to_{_endDate:yyyy-MM-dd}.xlsx";
		await JS.InvokeVoidAsync("saveAs", Convert.ToBase64String(memoryStream.ToArray()), fileName);
	}

	// Data classes for charts
	public class KitchenWiseData
	{
		public string KitchenName { get; set; }
		public decimal TotalQuantity { get; set; }
	}

	public class DailyProductionData
	{
		public string Date { get; set; }
		public decimal TotalQuantity { get; set; }
	}

	public class ProductCategoryData
	{
		public string CategoryName { get; set; }
		public int ProductCount { get; set; }
	}
}