using PrimeOrdersLibrary.Exporting.Sale;

using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.Data;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;

namespace PrimeOrders.Components.Pages.Reports.Sale;

public partial class DetailedReport
{
	[Parameter] public int? LocationId { get; set; }

	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] private IJSRuntime JS { get; set; }

	private UserModel _user;
	private bool _isLoading = true;
	private bool _saleSummaryVisible = false;

	private int _selectedLocationId = 0;

	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now);
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);

	private SaleOverviewModel _selectedSale;
	private readonly List<SaleDetailDisplayModel> _selectedSaleDetails = [];
	private List<LocationModel> _locations = [];
	private List<SaleOverviewModel> _saleOverviews = [];

	private SfGrid<SaleOverviewModel> _sfGrid;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_isLoading = true;

		if (!((_user = (await AuthService.ValidateUser(JS, NavManager)).User) is not null))
			return;

		if (LocationId.HasValue && LocationId.Value > 0 && _user.LocationId == 1)
			_selectedLocationId = LocationId.Value;

		await LoadData();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task DateRangeChanged(RangePickerEventArgs<DateOnly> args)
	{
		_startDate = args.StartDate;
		_endDate = args.EndDate;
		await LoadData();
	}

	private async Task OnLocationChanged(ChangeEventArgs<int, LocationModel> args)
	{
		_selectedLocationId = args.Value;
		await LoadData();
	}

	private async Task LoadData()
	{
		if (_user.LocationId == 1)
		{
			_locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location, true);

			if (_selectedLocationId == 0)
				_selectedLocationId = _locations.FirstOrDefault()?.Id ?? 0;
		}
		else
			_selectedLocationId = _user.LocationId;

		_saleOverviews = await SaleData.LoadSaleDetailsByDateLocationId(
			_startDate.ToDateTime(new TimeOnly(0, 0)),
			_endDate.ToDateTime(new TimeOnly(23, 59)),
			_selectedLocationId);

		StateHasChanged();
	}
	#endregion

	#region Sale Summary Module Methods
	private async Task OnSaleRowSelected(RowSelectEventArgs<SaleOverviewModel> args)
	{
		_selectedSale = args.Data;
		await LoadSaleDetails(_selectedSale.SaleId);
		_saleSummaryVisible = true;
		StateHasChanged();
	}

	private async Task LoadSaleDetails(int saleId)
	{
		_selectedSaleDetails.Clear();

		var saleDetails = await SaleData.LoadSaleDetailBySale(saleId);
		foreach (var detail in saleDetails)
		{
			var product = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, detail.ProductId);
			if (product is not null)
				_selectedSaleDetails.Add(new()
				{
					ProductName = product.Name,
					Quantity = detail.Quantity,
					Rate = detail.Rate,
					Total = detail.Total
				});
		}
	}

	private async Task OnSaleSummaryVisibilityChanged(bool isVisible)
	{
		_saleSummaryVisible = isVisible;
		await LoadData();
		StateHasChanged();
	}
	#endregion

	#region Excel Export
	private async Task ExportToExcel()
	{
		if (_saleOverviews is null || _saleOverviews.Count == 0)
		{
			await JS.InvokeVoidAsync("alert", "No data to export");
			return;
		}

		var memoryStream = SaleExcelExport.ExportSaleOverviewExcel(_saleOverviews, _startDate, _endDate);
		var fileName = $"Sales_Detail_{_startDate:yyyy-MM-dd}_to_{_endDate:yyyy-MM-dd}.xlsx";
		await JS.InvokeVoidAsync("saveExcel", Convert.ToBase64String(memoryStream.ToArray()), fileName);
	}
	#endregion

	#region Chart Data
	private List<DailySalesChartData> GetDailySalesData() =>
		[.. _saleOverviews
			.GroupBy(s => s.SaleDateTime.Date)
			.Select(group => new DailySalesChartData
			{
				Date = group.Key.ToString("dd/MM"),
				Amount = group.Sum(s => s.Total)
			})
			.OrderBy(d => DateTime.ParseExact(d.Date, "dd/MM", null))];

	private List<SalePaymentMethodChartData> GetPaymentMethodsData()
	{
		var paymentData = new List<SalePaymentMethodChartData>
		{
			new() { PaymentMethod = "Cash", Amount = _saleOverviews.Sum(s => s.Cash) },
			new() { PaymentMethod = "Card", Amount = _saleOverviews.Sum(s => s.Card) },
			new() { PaymentMethod = "UPI", Amount = _saleOverviews.Sum(s => s.UPI) },
			new() { PaymentMethod = "Credit", Amount = _saleOverviews.Sum(s => s.Credit) }
		};

		return [.. paymentData.Where(p => p.Amount > 0)];
	}
	#endregion
}