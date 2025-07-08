using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeOrders.Components.Pages.Reports.Sale;

public partial class SaleReturnReport
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	private UserModel _user;
	private bool _isLoading = true;
	private bool _deleteConfirmationDialogVisible = false;

	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30));
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);
	private int _selectedLocationId = 0;
	private int _saleReturnToDeleteId = 0;
	private string _saleReturnToDeleteTransactionNo = "";

	private List<SaleReturnOverviewModel> _saleReturnOverviews = [];
	private List<LocationModel> _locations = [];

	private SfGrid<SaleReturnOverviewModel> _sfGrid;
	private SfDialog _sfDeleteConfirmationDialog;
	private SfToast _sfSuccessToast;
	private SfToast _sfErrorToast;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_isLoading = true;

		if (!((_user = (await AuthService.ValidateUser(JS, NavManager, UserRoles.Sales, primaryLocationRequirement: true)).User) is not null))
			return;

		await LoadLocations();
		await LoadSaleReturnData();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadLocations()
	{
		if (_user.LocationId == 1)
		{
			_locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location);
			_locations.Insert(0, new LocationModel { Id = 0, Name = "All Locations" });
		}
	}

	private async Task LoadSaleReturnData()
	{
		if (_user.LocationId != 1)
			_selectedLocationId = _user.LocationId;

		_saleReturnOverviews = await SaleReturnData.LoadSaleReturnDetailsByDateLocationId(
				_startDate.ToDateTime(new TimeOnly(0, 0)),
				_endDate.ToDateTime(new TimeOnly(23, 59)),
				_selectedLocationId);
	}

	private async Task DateRangeChanged(RangePickerEventArgs<DateOnly> args)
	{
		_startDate = args.StartDate;
		_endDate = args.EndDate;
		await RefreshData();
	}

	private async Task OnLocationChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<int, LocationModel> args)
	{
		_selectedLocationId = args.Value;
		await RefreshData();
	}

	private async Task RefreshData()
	{
		_isLoading = true;
		StateHasChanged();

		await LoadSaleReturnData();

		_isLoading = false;
		StateHasChanged();
	}

	private void OnRowSelected(RowSelectEventArgs<SaleReturnOverviewModel> args) =>
			NavManager.NavigateTo($"/SaleReturn/{args.Data.SaleReturnId}");

	private void ShowDeleteConfirmation(int saleReturnId, string transactionNo)
	{
		if (!_user.Admin)
		{
			_sfErrorToast.Content = "Only administrators can delete records.";
			_sfErrorToast.ShowAsync();
			return;
		}

		_saleReturnToDeleteId = saleReturnId;
		_saleReturnToDeleteTransactionNo = transactionNo;
		_deleteConfirmationDialogVisible = true;
		StateHasChanged();
	}

	private async Task ConfirmDeleteSaleReturn()
	{
		var saleReturn = await CommonData.LoadTableDataById<SaleReturnModel>(TableNames.SaleReturn, _saleReturnToDeleteId);
		if (saleReturn is null)
		{
			_sfErrorToast.Content = "Sale return not found.";
			await _sfErrorToast.ShowAsync();
			return;
		}

		saleReturn.Status = false;
		await SaleReturnData.InsertSaleReturn(saleReturn);
		await StockData.DeleteProductStockByTransactionNo(saleReturn.TransactionNo);

		_sfSuccessToast.Content = "Sale return deleted successfully.";
		await _sfSuccessToast.ShowAsync();

		await LoadSaleReturnData();
		_deleteConfirmationDialogVisible = false;
		StateHasChanged();
	}

	private void CancelDelete()
	{
		_deleteConfirmationDialogVisible = false;
		_saleReturnToDeleteId = 0;
		_saleReturnToDeleteTransactionNo = "";
		StateHasChanged();
	}

	// Chart data methods
	private List<LocationWiseData> GetLocationWiseData() =>
		[.. _saleReturnOverviews
			.GroupBy(sr => sr.LocationName)
			.Select(g => new LocationWiseData
			{
				LocationName = g.Key,
				TotalQuantity = g.Sum(x => x.TotalQuantity)
			})
			.OrderByDescending(x => x.TotalQuantity)
			.Take(10)];

	private List<DailyReturnData> GetDailyReturnData() =>
		[.. _saleReturnOverviews
			.GroupBy(sr => DateOnly.FromDateTime(sr.ReturnDateTime))
			.Select(g => new DailyReturnData
			{
				Date = g.Key.ToString("dd/MM"),
				TotalQuantity = g.Sum(x => x.TotalQuantity)
			})
			.OrderBy(x => DateOnly.Parse(x.Date, new System.Globalization.CultureInfo("en-GB")))];

	private List<ProductCategoryData> GetProductCategoryData() =>
		[.. _saleReturnOverviews
			.GroupBy(sr => sr.LocationName)
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
		if (_saleReturnOverviews is null || _saleReturnOverviews.Count == 0)
		{
			await JS.InvokeVoidAsync("alert", "No data to export");
			return;
		}

		// Create summary items dictionary with key metrics
		Dictionary<string, object> summaryItems = new()
		{
			{ "Total Returns", _saleReturnOverviews.Count },
			{ "Total Products", _saleReturnOverviews.Sum(_ => _.TotalProducts) },
			{ "Total Quantity", _saleReturnOverviews.Sum(_ => _.TotalQuantity) },
			{ "Locations Active", _saleReturnOverviews.Select(_ => _.LocationId).Distinct().Count() }
		};

		// Add top locations summary data
		var topLocations = _saleReturnOverviews
			.GroupBy(sr => sr.LocationName)
			.OrderByDescending(g => g.Sum(x => x.TotalQuantity))
			.Take(3)
			.ToList();

		foreach (var location in topLocations)
			summaryItems.Add($"Location: {location.Key}", location.Sum(l => l.TotalQuantity));

		// Define the column order for better readability
		List<string> columnOrder = [
			nameof(SaleReturnOverviewModel.TransactionNo),
			nameof(SaleReturnOverviewModel.ReturnDateTime),
			nameof(SaleReturnOverviewModel.LocationName),
			nameof(SaleReturnOverviewModel.UserName),
			nameof(SaleReturnOverviewModel.OriginalBillNo),
			nameof(SaleReturnOverviewModel.TotalProducts),
			nameof(SaleReturnOverviewModel.TotalQuantity),
			nameof(SaleReturnOverviewModel.Remarks),
			nameof(SaleReturnOverviewModel.Status)
		];

		// Define custom column settings
		var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
		{
			[nameof(SaleReturnOverviewModel.TransactionNo)] = new()
			{
				DisplayName = "Transaction #",
				Width = 15,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(SaleReturnOverviewModel.ReturnDateTime)] = new()
			{
				DisplayName = "Return Date",
				Format = "dd-MMM-yyyy HH:mm",
				Width = 18,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(SaleReturnOverviewModel.LocationName)] = new()
			{
				DisplayName = "Location",
				Width = 20,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(SaleReturnOverviewModel.UserName)] = new()
			{
				DisplayName = "Processed By",
				Width = 18,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(SaleReturnOverviewModel.OriginalBillNo)] = new()
			{
				DisplayName = "Original Bill",
				Width = 15,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(SaleReturnOverviewModel.TotalProducts)] = new()
			{
				DisplayName = "Products",
				Format = "#,##0",
				Width = 10,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(SaleReturnOverviewModel.TotalQuantity)] = new()
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
						FontColor = qtyValue > 10 ? Syncfusion.Drawing.Color.FromArgb(220, 53, 69) : null
					};
				}
			},
			[nameof(SaleReturnOverviewModel.Remarks)] = new()
			{
				DisplayName = "Remarks",
				Width = 30,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(SaleReturnOverviewModel.Status)] = new()
			{
				DisplayName = "Active",
				Width = 10,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			}
		};

		// Generate title based on location selection if applicable
		string reportTitle = "Sale Return Report";

		if (_selectedLocationId > 0)
		{
			var location = _locations.FirstOrDefault(l => l.Id == _selectedLocationId);
			if (location != null)
				reportTitle = $"Sale Return Report - {location.Name}";
		}

		string worksheetName = "Return Details";

		var memoryStream = ExcelExportUtil.ExportToExcel(
			_saleReturnOverviews,
			reportTitle,
			worksheetName,
			_startDate,
			_endDate,
			summaryItems,
			columnSettings,
			columnOrder);

		// Generate appropriate filename
		string filenameSuffix = string.Empty;
		if (_selectedLocationId > 0)
		{
			var location = _locations.FirstOrDefault(l => l.Id == _selectedLocationId);
			if (location is not null)
				filenameSuffix = $"_{location.Name}";
		}

		var fileName = $"Sale_Return_Report{filenameSuffix}_{_startDate:yyyy-MM-dd}_to_{_endDate:yyyy-MM-dd}.xlsx";
		await JS.InvokeVoidAsync("saveAs", Convert.ToBase64String(memoryStream.ToArray()), fileName);
	}

	// Data classes for charts
	public class LocationWiseData
	{
		public string LocationName { get; set; }
		public decimal TotalQuantity { get; set; }
	}

	public class DailyReturnData
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