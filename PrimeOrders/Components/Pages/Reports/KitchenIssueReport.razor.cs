using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.Grids;

namespace PrimeOrders.Components.Pages.Reports;

public partial class KitchenIssueReport
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	private bool _isLoading = true;
	private UserModel _user;

	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30));
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);
	private int _selectedKitchenId = 0;

	private List<KitchenIssueOverviewModel> _kitchenIssueOverviews = [];
	private List<KitchenModel> _kitchens = [];

	private SfGrid<KitchenIssueOverviewModel> _sfGrid;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_isLoading = true;

		if (!((_user = (await AuthService.ValidateUser(JS, NavManager, UserRoles.Inventory, primaryLocationRequirement: true)).User) is not null))
			return;

		await LoadKitchens();
		await LoadKitchenIssueData();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadKitchens()
	{
		_kitchens = await CommonData.LoadTableDataByStatus<KitchenModel>(TableNames.Kitchen);
		// Add "All Kitchens" option
		_kitchens.Insert(0, new KitchenModel { Id = 0, Name = "All Kitchens" });
	}

	private async Task LoadKitchenIssueData()
	{
		_kitchenIssueOverviews = await KitchenIssueData.LoadKitchenIssueDetailsByDate(
			_startDate.ToDateTime(new TimeOnly(0, 0)),
			_endDate.ToDateTime(new TimeOnly(23, 59)));

		if (_selectedKitchenId > 0)
			_kitchenIssueOverviews = [.. _kitchenIssueOverviews.Where(i => i.KitchenId == _selectedKitchenId)];
	}

	private async Task DateRangeChanged(RangePickerEventArgs<DateOnly> args)
	{
		_startDate = args.StartDate;
		_endDate = args.EndDate;
		await RefreshData();
	}

	private async Task OnKitchenChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<int, KitchenModel> args)
	{
		_selectedKitchenId = args.Value;
		await RefreshData();
	}

	private async Task RefreshData()
	{
		_isLoading = true;
		StateHasChanged();

		await LoadKitchenIssueData();

		_isLoading = false;
		StateHasChanged();
	}

	private void OnRowSelected(RowSelectEventArgs<KitchenIssueOverviewModel> args) =>
			NavManager.NavigateTo($"/Inventory/Kitchen-Issue/{args.Data.KitchenIssueId}");

	// Chart data methods
	private List<KitchenWiseData> GetKitchenWiseData()
	{
		return [.. _kitchenIssueOverviews
			.GroupBy(i => i.KitchenName)
			.Select(g => new KitchenWiseData
			{
				KitchenName = g.Key,
				TotalQuantity = g.Sum(x => x.TotalQuantity)
			})
			.OrderByDescending(x => x.TotalQuantity)
			.Take(10)];
	}

	private List<KitchenWiseData> GetKitchenDistributionData()
	{
		return GetKitchenWiseData();
	}

	private List<DailyIssueData> GetDailyIssueData() =>
		[.. _kitchenIssueOverviews
			.GroupBy(i => DateOnly.FromDateTime(i.IssueDate))
			.Select(g => new DailyIssueData
			{
				Date = g.Key.ToString("dd/MM"),
				TotalQuantity = g.Sum(x => x.TotalQuantity)
			})
			.OrderBy(x => DateOnly.Parse(x.Date, new System.Globalization.CultureInfo("en-GB")))];

	private List<KitchenIssueCountData> GetKitchenIssueCountData() =>
		[.. _kitchenIssueOverviews
			.GroupBy(i => i.KitchenName)
			.Select(g => new KitchenIssueCountData
			{
				KitchenName = g.Key,
				IssueCount = g.Count()
			})
			.OrderByDescending(x => x.IssueCount)
			.Take(10)];

	private async Task ExportToPdf() =>
		await _sfGrid.ExportToPdfAsync();

	private async Task ExportToExcel()
	{
		if (_kitchenIssueOverviews is null || _kitchenIssueOverviews.Count == 0)
		{
			await JS.InvokeVoidAsync("alert", "No data to export");
			return;
		}

		// Create summary items dictionary with key metrics
		Dictionary<string, object> summaryItems = new()
		{
			{ "Total Transactions", _kitchenIssueOverviews.Count },
			{ "Total Products", _kitchenIssueOverviews.Sum(_ => _.TotalProducts) },
			{ "Total Quantity", _kitchenIssueOverviews.Sum(_ => _.TotalQuantity) },
			{ "Kitchens Served", _kitchenIssueOverviews.Select(_ => _.KitchenId).Distinct().Count() }
		};

		// Add top kitchens summary data
		var topKitchens = _kitchenIssueOverviews
			.GroupBy(i => i.KitchenName)
			.OrderByDescending(g => g.Sum(x => x.TotalQuantity))
			.Take(3)
			.ToList();

		foreach (var kitchen in topKitchens)
			summaryItems.Add($"Kitchen: {kitchen.Key}", kitchen.Sum(k => k.TotalQuantity));

		// Define the column order for better readability
		List<string> columnOrder = [
			nameof(KitchenIssueOverviewModel.TransactionNo),
		nameof(KitchenIssueOverviewModel.IssueDate),
		nameof(KitchenIssueOverviewModel.KitchenName),
		nameof(KitchenIssueOverviewModel.UserName),
		nameof(KitchenIssueOverviewModel.TotalProducts),
		nameof(KitchenIssueOverviewModel.TotalQuantity),
		nameof(KitchenIssueOverviewModel.Status)
		];

		// Define custom column settings
		var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
		{
			[nameof(KitchenIssueOverviewModel.TransactionNo)] = new()
			{
				DisplayName = "Transaction #",
				Width = 15,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(KitchenIssueOverviewModel.IssueDate)] = new()
			{
				DisplayName = "Issue Date",
				Format = "dd-MMM-yyyy",
				Width = 15,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(KitchenIssueOverviewModel.KitchenName)] = new()
			{
				DisplayName = "Kitchen",
				Width = 20,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(KitchenIssueOverviewModel.UserName)] = new()
			{
				DisplayName = "Issued By",
				Width = 18,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(KitchenIssueOverviewModel.TotalProducts)] = new()
			{
				DisplayName = "Products",
				Format = "#,##0",
				Width = 10,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(KitchenIssueOverviewModel.TotalQuantity)] = new()
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
		string reportTitle = "Kitchen Issue Report";

		if (_selectedKitchenId > 0)
		{
			var kitchen = _kitchens.FirstOrDefault(k => k.Id == _selectedKitchenId);
			if (kitchen != null)
				reportTitle = $"Kitchen Issue Report - {kitchen.Name}";
		}

		string worksheetName = "Issue Details";

		var memoryStream = ExcelExportUtil.ExportToExcel(
			_kitchenIssueOverviews,
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

		var fileName = $"Kitchen_Issue_Report{filenameSuffix}_{_startDate:yyyy-MM-dd}_to_{_endDate:yyyy-MM-dd}.xlsx";
		await JS.InvokeVoidAsync("saveAs", Convert.ToBase64String(memoryStream.ToArray()), fileName);
	}

	// Data classes for charts
	public class KitchenWiseData
	{
		public string KitchenName { get; set; }
		public decimal TotalQuantity { get; set; }
	}

	public class DailyIssueData
	{
		public string Date { get; set; }
		public decimal TotalQuantity { get; set; }
	}

	public class KitchenIssueCountData
	{
		public string KitchenName { get; set; }
		public int IssueCount { get; set; }
	}
}