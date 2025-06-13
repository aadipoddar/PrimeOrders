using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;

namespace PrimeOrders.Components.Pages.Reports;

public partial class PurchaseReport
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] private IJSRuntime JS { get; set; }

	private UserModel _user;
	private bool _isLoading = true;

	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30)); // Default to last 30 days
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);

	private List<SupplierModel> _suppliers = [];
	private int _selectedSupplierId;
	private List<PurchaseOverviewModel> _purchaseOverviews = [];
	private List<PurchaseOverviewModel> _filteredPurchaseOverviews => ApplyFilters();

	private SfGrid<PurchaseOverviewModel> _sfGrid;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		_isLoading = true;

		if (firstRender && !await ValidatePassword())
			NavManager.NavigateTo("/Login");

		if (_user.LocationId != 1)
			NavManager.NavigateTo("/Report-Dashboard");

		_isLoading = false;

		StateHasChanged();

		if (firstRender)
			await LoadInitialData();
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

	private async Task LoadInitialData()
	{
		_suppliers = await CommonData.LoadTableDataByStatus<SupplierModel>(TableNames.Supplier, true);
		await LoadPurchaseData();
	}

	private async Task LoadPurchaseData()
	{
		_purchaseOverviews = await PurchaseData.LoadPurchaseDetailsByDate(
			_startDate.ToDateTime(new TimeOnly(0, 0)),
			_endDate.ToDateTime(new TimeOnly(23, 59)));

		StateHasChanged();
	}

	private List<PurchaseOverviewModel> ApplyFilters()
	{
		if (_purchaseOverviews is null)
			return [];

		var filtered = _purchaseOverviews;

		if (_selectedSupplierId > 0)
			filtered = [.. filtered.Where(p => p.SupplierId == _selectedSupplierId)];

		return filtered;
	}

	private void NavigateTo(string route) =>
		NavManager.NavigateTo(route);

	private async Task Logout()
	{
		await JS.InvokeVoidAsync("deleteCookie", "UserId");
		await JS.InvokeVoidAsync("deleteCookie", "Passcode");
		NavManager.NavigateTo("/Login");
	}

	private async Task DateRangeChanged(RangePickerEventArgs<DateOnly> args)
	{
		_startDate = args.StartDate;
		_endDate = args.EndDate;
		await LoadPurchaseData();
	}

	private void OnSupplierChanged(ChangeEventArgs<int, SupplierModel> args)
	{
		_selectedSupplierId = args.Value;
		StateHasChanged();
	}

	public void PurchaseHistoryRowSelected(RowSelectEventArgs<PurchaseOverviewModel> args) =>
		NavigateTo($"/Inventory/Purchase/{args.Data.PurchaseId}");

	private async Task ExportToPdf()
	{
		if (_sfGrid != null)
			await _sfGrid.ExportToPdfAsync();
	}

	private async Task ExportToExcel()
	{
		if (_filteredPurchaseOverviews == null || _filteredPurchaseOverviews.Count == 0)
		{
			await JS.InvokeVoidAsync("alert", "No data to export");
			return;
		}

		// Create summary items dictionary
		Dictionary<string, object> summaryItems = new()
		{
			{ "Total Purchases", _filteredPurchaseOverviews.Sum(p => p.Total) },
			{ "Total Transactions", _filteredPurchaseOverviews.Count },
			{ "Total Items", _filteredPurchaseOverviews.Sum(p => p.TotalItems) },
			{ "Total Quantity", _filteredPurchaseOverviews.Sum(p => p.TotalQuantity) },
			{ "Total Discount", _filteredPurchaseOverviews.Sum(p => p.DiscountAmount) },
			{ "Total Tax", _filteredPurchaseOverviews.Sum(p => p.TotalTaxAmount) }
		};

		// Define the column order for better readability
		List<string> columnOrder = [
			nameof(PurchaseOverviewModel.BillNo),
			nameof(PurchaseOverviewModel.BillDate),
			nameof(PurchaseOverviewModel.SupplierName),
			nameof(PurchaseOverviewModel.TotalItems),
			nameof(PurchaseOverviewModel.TotalQuantity),
			nameof(PurchaseOverviewModel.BaseTotal),
			nameof(PurchaseOverviewModel.DiscountAmount),
			nameof(PurchaseOverviewModel.TotalTaxAmount),
			nameof(PurchaseOverviewModel.Total)
		];

		// Define custom column settings
		var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
		{
			[nameof(PurchaseOverviewModel.BillNo)] = new()
			{
				DisplayName = "Invoice No",
				Width = 15
			},
			[nameof(PurchaseOverviewModel.BillDate)] = new()
			{
				DisplayName = "Purchase Date",
				Format = "dd-MMM-yyyy",
				Width = 15
			},
			[nameof(PurchaseOverviewModel.SupplierName)] = new()
			{
				DisplayName = "Supplier",
				Width = 25,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(PurchaseOverviewModel.TotalItems)] = new()
			{
				DisplayName = "Items Count",
				Format = "#,##0",
				Width = 12,
				IncludeInTotal = true
			},
			[nameof(PurchaseOverviewModel.TotalQuantity)] = new()
			{
				DisplayName = "Total Quantity",
				Format = "#,##0.00",
				Width = 15,
				IncludeInTotal = true
			},
			[nameof(PurchaseOverviewModel.BaseTotal)] = new()
			{
				DisplayName = "Base Total",
				Format = "₹#,##0.00",
				Width = 15,
				IsCurrency = true,
				IncludeInTotal = true
			},
			[nameof(PurchaseOverviewModel.DiscountAmount)] = new()
			{
				DisplayName = "Discount",
				Format = "₹#,##0.00",
				Width = 15,
				IsCurrency = true,
				IncludeInTotal = true
			},
			[nameof(PurchaseOverviewModel.TotalTaxAmount)] = new()
			{
				DisplayName = "Total Tax",
				Format = "₹#,##0.00",
				Width = 15,
				IsCurrency = true,
				IncludeInTotal = true
			},
			[nameof(PurchaseOverviewModel.Total)] = new()
			{
				DisplayName = "Total",
				Format = "₹#,##0.00",
				Width = 15,
				IsCurrency = true,
				IncludeInTotal = true,
				HighlightNegative = true
			}
		};

		// Generate title based on selected filters
		string reportTitle = "Purchase Report";

		if (_selectedSupplierId > 0)
		{
			var supplier = _suppliers.FirstOrDefault(s => s.Id == _selectedSupplierId);
			if (supplier != null)
				reportTitle = $"Purchase Report - {supplier.Name}";
		}

		string worksheetName = "Purchase Details";

		var memoryStream = ExcelExportUtil.ExportToExcel(
			_filteredPurchaseOverviews,
			reportTitle,
			worksheetName,
			_startDate,
			_endDate,
			summaryItems,
			columnSettings,
			columnOrder);

		var fileName = $"Purchase_Report_{_startDate:yyyy-MM-dd}_to_{_endDate:yyyy-MM-dd}.xlsx";
		await JS.InvokeVoidAsync("saveAs", Convert.ToBase64String(memoryStream.ToArray()), fileName);
	}

	#region Chart Data Methods

	private List<DailyPurchaseData> GetDailyPurchaseData()
	{
		if (_filteredPurchaseOverviews == null || _filteredPurchaseOverviews.Count == 0)
			return [];

		var result = _filteredPurchaseOverviews
			.GroupBy(p => p.BillDate)
			.Select(group => new DailyPurchaseData
			{
				Date = group.Key.ToString("dd/MM"),
				Amount = group.Sum(p => p.Total)
			})
			.OrderBy(d => DateTime.ParseExact(d.Date, "dd/MM", null))
			.ToList();

		return result;
	}

	private List<SupplierDistributionData> GetVendorDistributionData()
	{
		if (_filteredPurchaseOverviews == null || _filteredPurchaseOverviews.Count == 0)
			return [];

		var result = _filteredPurchaseOverviews
			.GroupBy(p => p.SupplierName)
			.Select(group => new SupplierDistributionData
			{
				SupplierName = group.Key,
				Amount = group.Sum(p => p.Total)
			})
			.OrderByDescending(v => v.Amount)
			.Take(10) // Get top 10 suppliers
			.ToList();

		return result;
	}

	#endregion

	#region Data Models

	public class DailyPurchaseData
	{
		public string Date { get; set; }
		public decimal Amount { get; set; }
	}

	public class SupplierDistributionData
	{
		public string SupplierName { get; set; }
		public decimal Amount { get; set; }
	}

	#endregion
}