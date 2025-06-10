using PrimeOrders.Components.Pages.Reports;

using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.Grids;

namespace PrimeOrders.Components.Pages.Inventory;

public partial class PurchaseHistory
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] private IJSRuntime JS { get; set; }

	private UserModel _user;
	private bool _isLoading = true;

	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now);
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);

	private List<PurchaseOverviewModel> _purchaseOverviews = [];

	private SfGrid<PurchaseOverviewModel> _sfGrid;

	protected override async Task OnInitializedAsync()
	{
		_isLoading = true;

		if (!await ValidatePassword())
		{
			NavManager.NavigateTo("/Login");
			return;
		}

		await LoadData();
		_isLoading = false;
	}

	private async Task<bool> ValidatePassword()
	{
		var userId = await JS.InvokeAsync<string>("getCookie", "UserId");
		var password = await JS.InvokeAsync<string>("getCookie", "Passcode");

		if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
			return false;

		var user = await CommonData.LoadTableDataById<UserModel>(TableNames.User, int.Parse(userId));
		if (user is null || !BCrypt.Net.BCrypt.EnhancedVerify(user.Passcode.ToString(), password))
			return false;

		_user = user;
		return true;
	}

	private async Task DateRangeChanged(RangePickerEventArgs<DateOnly> args)
	{
		_startDate = args.StartDate;
		_endDate = args.EndDate;
		await LoadData();
	}

	private async Task LoadData()
	{
		_purchaseOverviews = await PurchaseData.LoadPurchaseDetailsByDate(
			_startDate.ToDateTime(new TimeOnly(0, 0)),
			_endDate.ToDateTime(new TimeOnly(23, 59)));

		StateHasChanged();
	}

	public void PurchaseHistoryRowSelected(RowSelectEventArgs<PurchaseOverviewModel> args) =>
		NavigateTo($"/Inventory/Purchase/{args.Data.PurchaseId}");

	private async Task ExportToPdf() =>
		await _sfGrid?.ExportToPdfAsync();

	private async Task ExportToExcel()
	{
		if (_purchaseOverviews is null || _purchaseOverviews.Count == 0)
		{
			await JS.InvokeVoidAsync("alert", "No data to export");
			return;
		}

		// Create summary items dictionary
		Dictionary<string, object> summaryItems = new()
		{
			{ "Total Purchases", _purchaseOverviews.Sum(p => p.Total) },
			{ "Total Transactions", _purchaseOverviews.Count },
			{ "Total Items", _purchaseOverviews.Sum(p => p.TotalItems) },
			{ "Total Quantity", _purchaseOverviews.Sum(p => p.TotalQuantity) },
			{ "Discount Amount", _purchaseOverviews.Sum(p => p.DiscountAmount) },
			{ "SGST Amount", _purchaseOverviews.Sum(p => p.SGSTAmount) },
			{ "CGST Amount", _purchaseOverviews.Sum(p => p.CGSTAmount) },
			{ "IGST Amount", _purchaseOverviews.Sum(p => p.IGSTAmount) }
		};

		// Define the column order for better readability
		List<string> columnOrder = [
					nameof(PurchaseOverviewModel.PurchaseId),
					nameof(PurchaseOverviewModel.BillDate),
					nameof(PurchaseOverviewModel.BillNo),
					nameof(PurchaseOverviewModel.SupplierName),
					nameof(PurchaseOverviewModel.UserName),
					nameof(PurchaseOverviewModel.TotalItems),
					nameof(PurchaseOverviewModel.TotalQuantity),
					nameof(PurchaseOverviewModel.BaseTotal),
					nameof(PurchaseOverviewModel.DiscountAmount),
					nameof(PurchaseOverviewModel.TotalTaxAmount),
					nameof(PurchaseOverviewModel.Total)
			];

		// Create a customized column settings for the report
		Dictionary<string, ExcelExportUtil.ColumnSetting> columnSettings = null;

		// Generate the Excel file
		string reportTitle = "Purchase History Report";
		string worksheetName = "Purchase Details";

		var memoryStream = ExcelExportUtil.ExportToExcel(
			_purchaseOverviews,
			reportTitle,
			worksheetName,
			_startDate,
			_endDate,
			summaryItems,
			columnSettings,
			columnOrder);

		var fileName = $"Purchase_History_{_startDate:yyyy-MM-dd}_to_{_endDate:yyyy-MM-dd}.xlsx";
		await JS.InvokeVoidAsync("saveAs", Convert.ToBase64String(memoryStream.ToArray()), fileName);
	}

	private void NavigateTo(string route) =>
		NavManager.NavigateTo(route);

	private async Task Logout()
	{
		await JS.InvokeVoidAsync("deleteCookie", "UserId");
		await JS.InvokeVoidAsync("deleteCookie", "Passcode");
		NavManager.NavigateTo("/Login");
	}
}