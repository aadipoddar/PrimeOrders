using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.Data;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeOrders.Components.Pages.Reports;

public partial class DetailedReport
{
	[Parameter] public int? LocationId { get; set; }

	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] private IJSRuntime JS { get; set; }

	private UserModel _user;
	private bool _isLoading = true;
	private bool _deleteConfirmationDialogVisible = false;

	private int _selectedLocationId = 0;
	private int _saleToDeleteId = 0;
	private string _saleToDeleteBillNo = "";

	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now);
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);

	private List<LocationModel> _locations = [];
	private List<SaleOverviewModel> _saleOverviews = [];

	private SfGrid<SaleOverviewModel> _sfGrid;
	private SfDialog _sfDeleteConfirmationDialog;
	private SfToast _sfSuccessToast;
	private SfToast _sfErrorToast;

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

	public void SaleHistoryRowSelected(RowSelectEventArgs<SaleOverviewModel> args) =>
		NavManager.NavigateTo($"/Sale/{args.Data.SaleId}");

	private void ShowDeleteConfirmation(int saleId, string billNo)
	{
		if (!_user.Admin)
		{
			_sfErrorToast.Content = "Only administrators can delete records.";
			_sfErrorToast.ShowAsync();
			return;
		}

		_saleToDeleteId = saleId;
		_saleToDeleteBillNo = billNo;
		_deleteConfirmationDialogVisible = true;
		StateHasChanged();
	}

	private async Task ConfirmDeleteSale()
	{
		var sale = await CommonData.LoadTableDataById<SaleModel>(TableNames.Sale, _saleToDeleteId);
		if (sale is null)
		{
			_sfErrorToast.Content = "Sale not found.";
			await _sfErrorToast.ShowAsync();
			return;
		}

		sale.Status = false;
		await SaleData.InsertSale(sale);
		await StockData.DeleteProductStockByTransactionNo(sale.BillNo);

		_sfSuccessToast.Content = "Sale Deleted Successfully.";
		await _sfSuccessToast.ShowAsync();

		await LoadData();
		_deleteConfirmationDialogVisible = false;
		StateHasChanged();
	}

	private void CancelDelete()
	{
		_deleteConfirmationDialogVisible = false;
		_saleToDeleteId = 0;
		_saleToDeleteBillNo = "";
		StateHasChanged();
	}

	private async Task ExportToExcel()
	{
		if (_saleOverviews is null || _saleOverviews.Count == 0)
		{
			await JS.InvokeVoidAsync("alert", "No data to export");
			return;
		}

		var memoryStream = SaleExcelExport.ExportSaleOverviewExcel(_saleOverviews, _startDate, _endDate);
		var fileName = $"Sales_Detail_{_startDate:yyyy-MM-dd}_to_{_endDate:yyyy-MM-dd}.xlsx";
		await JS.InvokeVoidAsync("saveAs", Convert.ToBase64String(memoryStream.ToArray()), fileName);
	}

	private List<DailySalesData> GetDailySalesData()
	{
		var result = _saleOverviews
			.GroupBy(s => s.SaleDateTime.Date)
			.Select(group => new DailySalesData
			{
				Date = group.Key.ToString("dd/MM"),
				Amount = group.Sum(s => s.Total)
			})
			.OrderBy(d => DateTime.ParseExact(d.Date, "dd/MM", null))
			.ToList();

		return result;
	}

	private List<PaymentMethodData> GetPaymentMethodsData()
	{
		var paymentData = new List<PaymentMethodData>
		{
			new() { PaymentMethod = "Cash", Amount = _saleOverviews.Sum(s => s.Cash) },
			new() { PaymentMethod = "Card", Amount = _saleOverviews.Sum(s => s.Card) },
			new() { PaymentMethod = "UPI", Amount = _saleOverviews.Sum(s => s.UPI) },
			new() { PaymentMethod = "Credit", Amount = _saleOverviews.Sum(s => s.Credit) }
		};

		return [.. paymentData.Where(p => p.Amount > 0)];
	}

	public class PaymentMethodData
	{
		public string PaymentMethod { get; set; }
		public decimal Amount { get; set; }
	}

	public class DailySalesData
	{
		public string Date { get; set; }
		public decimal Amount { get; set; }
	}
}