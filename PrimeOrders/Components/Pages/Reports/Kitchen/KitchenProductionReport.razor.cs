using PrimeBakesLibrary.Data.Inventory.Kitchen;
using PrimeBakesLibrary.Exporting.Kitchen;

using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.Grids;

namespace PrimeOrders.Components.Pages.Reports.Kitchen;

public partial class KitchenProductionReport
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	private bool _isLoading = true;
	private UserModel _user;
	private bool _kitchenProductionSummaryVisible = false;

	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30));
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);
	private int _selectedKitchenId = 0;

	private List<KitchenProductionOverviewModel> _kitchenProductionOverviews = [];
	private List<KitchenModel> _kitchens = [];

	private KitchenProductionOverviewModel _selectedKitchenProduction;
	private readonly List<KitchenProductionDetailDisplayModel> _selectedKitchenProductionDetails = [];

	private SfGrid<KitchenProductionOverviewModel> _sfGrid;

	#region Load Data
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
		_kitchens.Insert(0, new KitchenModel { Id = 0, Name = "All Kitchens" });
	}

	private async Task LoadKitchenProductionData()
	{
		_kitchenProductionOverviews = await KitchenProductionData.LoadKitchenProductionDetailsByDate(
			_startDate.ToDateTime(new TimeOnly(0, 0)),
			_endDate.ToDateTime(new TimeOnly(23, 59)));

		if (_selectedKitchenId > 0)
			_kitchenProductionOverviews = [.. _kitchenProductionOverviews.Where(i => i.KitchenId == _selectedKitchenId)];
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

		await LoadKitchenProductionData();

		_isLoading = false;
		StateHasChanged();
	}
	#endregion

	#region Kitchen Production Summary Module Methods
	private async Task OnRowSelected(RowSelectEventArgs<KitchenProductionOverviewModel> args)
	{
		_selectedKitchenProduction = args.Data;
		await LoadKitchenProductionDetails(_selectedKitchenProduction.KitchenProductionId);
		_kitchenProductionSummaryVisible = true;
		StateHasChanged();
	}

	private async Task LoadKitchenProductionDetails(int kitchenProductionId)
	{
		_selectedKitchenProductionDetails.Clear();

		var kitchenProductionDetails = await KitchenProductionData.LoadKitchenProductionDetailByKitchenProduction(kitchenProductionId);
		foreach (var detail in kitchenProductionDetails)
		{
			var product = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, detail.ProductId);
			if (product is not null)
				_selectedKitchenProductionDetails.Add(new()
				{
					ProductName = product.Name,
					Quantity = detail.Quantity,
				});
		}
	}

	private async Task OnKitchenProductionSummaryVisibilityChanged(bool isVisible)
	{
		_kitchenProductionSummaryVisible = isVisible;
		await LoadKitchenProductionData();
		StateHasChanged();
	}
	#endregion

	#region Excel Export
	private async Task ExportToExcel()
	{
		if (_kitchenProductionOverviews is null || _kitchenProductionOverviews.Count == 0)
		{
			await JS.InvokeVoidAsync("alert", "No data to export");
			return;
		}

		var memoryStream = await KitchenProductionExcelExport.ExportKitchenProductionOverviewExcel(_kitchenProductionOverviews, _startDate, _endDate, _selectedKitchenId);
		var fileName = $"Kitchen_Production_Report_{_startDate:yyyy-MM-dd}_to_{_endDate:yyyy-MM-dd}.xlsx";
		await JS.InvokeVoidAsync("saveExcel", Convert.ToBase64String(memoryStream.ToArray()), fileName);
	}
	#endregion

	#region Chart Data
	private List<KitchenWiseProductionChartData> GetKitchenWiseData()
	{
		return [.. _kitchenProductionOverviews
			.GroupBy(i => i.KitchenName)
			.Select(g => new KitchenWiseProductionChartData
			{
				KitchenName = g.Key,
				TotalQuantity = g.Sum(x => x.TotalQuantity)
			})
			.OrderByDescending(x => x.TotalQuantity)
			.Take(10)];
	}

	private List<DailyProductionChartData> GetDailyProductionData() =>
		[.. _kitchenProductionOverviews
			.GroupBy(i => DateOnly.FromDateTime(i.ProductionDate))
			.Select(g => new DailyProductionChartData
			{
				Date = g.Key.ToString("dd/MM"),
				TotalQuantity = g.Sum(x => x.TotalQuantity)
			})
			.OrderBy(x => DateOnly.Parse(x.Date, new System.Globalization.CultureInfo("en-GB")))];

	private List<KitchenProductionCountChartData> GetKitchenProductionCountData() =>
		[.. _kitchenProductionOverviews
			.GroupBy(i => i.KitchenName)
			.Select(g => new KitchenProductionCountChartData
			{
				KitchenName = g.Key,
				ProductionCount = g.Count()
			})
			.OrderByDescending(x => x.ProductionCount)
			.Take(10)];
	#endregion
}