using PrimeBakesLibrary.Data.Inventory.Kitchen;
using PrimeBakesLibrary.Exporting.Kitchen;

using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.Grids;

namespace PrimeOrders.Components.Pages.Reports.Kitchen;

public partial class KitchenIssueReport
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	private bool _isLoading = true;
	private UserModel _user;
	private bool _kitchenIssueSummaryVisible = false;

	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30));
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);
	private int _selectedKitchenId = 0;

	private List<KitchenIssueOverviewModel> _kitchenIssueOverviews = [];
	private List<KitchenModel> _kitchens = [];

	private KitchenIssueOverviewModel _selectedKitchenIssue;
	private readonly List<KitchenIssueDetailDisplayModel> _selectedKitchenIssueDetails = [];

	private SfGrid<KitchenIssueOverviewModel> _sfGrid;

	#region Load Data
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
	#endregion

	#region Kitchen Issue Summary Module Methods
	private async Task OnRowSelected(RowSelectEventArgs<KitchenIssueOverviewModel> args)
	{
		_selectedKitchenIssue = args.Data;
		await LoadKitchenIssueDetails(_selectedKitchenIssue.KitchenIssueId);
		_kitchenIssueSummaryVisible = true;
		StateHasChanged();
	}

	private async Task LoadKitchenIssueDetails(int kitchenIssueId)
	{
		_selectedKitchenIssueDetails.Clear();

		var kitchenIssueDetails = await KitchenIssueData.LoadKitchenIssueDetailByKitchenIssue(kitchenIssueId);
		foreach (var detail in kitchenIssueDetails)
		{
			var rawMaterial = await CommonData.LoadTableDataById<RawMaterialModel>(TableNames.RawMaterial, detail.RawMaterialId);
			if (rawMaterial is not null)
				_selectedKitchenIssueDetails.Add(new()
				{
					RawMaterialName = rawMaterial.Name,
					Quantity = detail.Quantity,
					Unit = rawMaterial.MeasurementUnit ?? "Unit"
				});
		}
	}

	private async Task OnKitchenIssueSummaryVisibilityChanged(bool isVisible)
	{
		_kitchenIssueSummaryVisible = isVisible;
		await LoadKitchenIssueData();
		StateHasChanged();
	}
	#endregion

	#region Excel Export
	private async Task ExportToExcel()
	{
		if (_kitchenIssueOverviews is null || _kitchenIssueOverviews.Count == 0)
		{
			await JS.InvokeVoidAsync("alert", "No data to export");
			return;
		}

		var memoryStream = await KitchenIssueExcelExport.ExportKitchenIssueOverviewExcel(_kitchenIssueOverviews, _startDate, _endDate, _selectedKitchenId);
		var fileName = $"Kitchen_Issue_Report_{_startDate:yyyy-MM-dd}_to_{_endDate:yyyy-MM-dd}.xlsx";
		await JS.InvokeVoidAsync("saveExcel", Convert.ToBase64String(memoryStream.ToArray()), fileName);
	}
	#endregion

	#region Chart Data
	private List<KitchenWiseIssueChartData> GetKitchenWiseData()
	{
		return [.. _kitchenIssueOverviews
			.GroupBy(i => i.KitchenName)
			.Select(g => new KitchenWiseIssueChartData
			{
				KitchenName = g.Key,
				TotalQuantity = g.Sum(x => x.TotalQuantity)
			})
			.OrderByDescending(x => x.TotalQuantity)
			.Take(10)];
	}

	private List<DailyIssueChartData> GetDailyIssueData() =>
		[.. _kitchenIssueOverviews
			.GroupBy(i => DateOnly.FromDateTime(i.IssueDate))
			.Select(g => new DailyIssueChartData
			{
				Date = g.Key.ToString("dd/MM"),
				TotalQuantity = g.Sum(x => x.TotalQuantity)
			})
			.OrderBy(x => DateOnly.Parse(x.Date, new System.Globalization.CultureInfo("en-GB")))];

	private List<KitchenIssueCountChartData> GetKitchenIssueCountData() =>
		[.. _kitchenIssueOverviews
			.GroupBy(i => i.KitchenName)
			.Select(g => new KitchenIssueCountChartData
			{
				KitchenName = g.Key,
				IssueCount = g.Count()
			})
			.OrderByDescending(x => x.IssueCount)
			.Take(10)];
	#endregion
}