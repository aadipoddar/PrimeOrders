using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeOrders.Components.Pages.Reports;

public partial class PurchaseReport
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] private IJSRuntime JS { get; set; }

	private UserModel _user;
	private bool _isLoading = true;
	private bool _deleteConfirmationDialogVisible = false;

	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30));
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);

	private List<SupplierModel> _suppliers = [];
	private List<PurchaseOverviewModel> _purchaseOverviews = [];
	private int _selectedSupplierId = 0;
	private int _purchaseToDeleteId = 0;
	private string _purchaseToDeleteBillNo = "";

	private SfGrid<PurchaseOverviewModel> _sfGrid;
	private SfDialog _sfDeleteConfirmationDialog;
	private SfToast _sfSuccessToast;
	private SfToast _sfErrorToast;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_isLoading = true;

		if (!((_user = (await AuthService.ValidateUser(JS, NavManager, UserRoles.Inventory)).User) is not null))
			return;

		await LoadSuppliers();
		await LoadData();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadSuppliers()
	{
		_suppliers = await CommonData.LoadTableDataByStatus<SupplierModel>(TableNames.Supplier, true);
		_suppliers.Insert(0, new SupplierModel { Id = 0, Name = "All Suppliers" });
		_selectedSupplierId = 0;
	}

	private async Task DateRangeChanged(RangePickerEventArgs<DateOnly> args)
	{
		_startDate = args.StartDate;
		_endDate = args.EndDate;
		await LoadData();
	}

	private async Task OnSupplierChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<int, SupplierModel> args)
	{
		_selectedSupplierId = args.Value;
		await LoadData();
	}

	private async Task LoadData()
	{
		var allPurchases = await PurchaseData.LoadPurchaseDetailsByDate(
			_startDate.ToDateTime(new TimeOnly(0, 0)),
			_endDate.ToDateTime(new TimeOnly(23, 59)));

		if (_selectedSupplierId > 0)
			_purchaseOverviews = [.. allPurchases.Where(p => p.SupplierId == _selectedSupplierId)];
		else
			_purchaseOverviews = allPurchases;

		StateHasChanged();
	}

	public void PurchaseHistoryRowSelected(RowSelectEventArgs<PurchaseOverviewModel> args) =>
		ViewPurchaseDetails(args.Data.PurchaseId);

	private void ViewPurchaseDetails(int purchaseId) =>
		NavManager.NavigateTo($"/Inventory/Purchase/{purchaseId}");

	private void ShowDeleteConfirmation(int purchaseId, string billNo)
	{
		if (!_user.Admin)
		{
			_sfErrorToast.Content = "Only administrators can delete records.";
			_sfErrorToast.ShowAsync();
			return;
		}

		_purchaseToDeleteId = purchaseId;
		_purchaseToDeleteBillNo = billNo;
		_deleteConfirmationDialogVisible = true;
		StateHasChanged();
	}

	private async Task ConfirmDeletePurchase()
	{
		var purchase = await CommonData.LoadTableDataById<PurchaseModel>(TableNames.Purchase, _purchaseToDeleteId);
		if (purchase is null)
		{
			_sfErrorToast.Content = "Purchase not found.";
			await _sfErrorToast.ShowAsync();
			return;
		}

		purchase.Status = false;
		await PurchaseData.InsertPurchase(purchase);
		await StockData.DeleteRawMaterialStockByTransactionNo(purchase.BillNo);

		_sfSuccessToast.Content = "Purchase deactivated successfully.";
		await _sfSuccessToast.ShowAsync();

		await LoadData();
		_deleteConfirmationDialogVisible = false;
		StateHasChanged();
	}

	private void CancelDelete()
	{
		_deleteConfirmationDialogVisible = false;
		_purchaseToDeleteId = 0;
		_purchaseToDeleteBillNo = "";
		StateHasChanged();
	}

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
			{ "Total Discount", _purchaseOverviews.Sum(p => p.DiscountAmount) },
			{ "Total Tax", _purchaseOverviews.Sum(p => p.TotalTaxAmount) },
			{ "Base Total", _purchaseOverviews.Sum(p => p.BaseTotal) },
			{ "Sub Total", _purchaseOverviews.Sum(p => p.SubTotal) }
		};

		// Add supplier filter info if specific supplier is selected
		if (_selectedSupplierId > 0)
		{
			var supplierName = _suppliers.FirstOrDefault(s => s.Id == _selectedSupplierId)?.Name ?? "Unknown";
			summaryItems.Add("Filtered by Supplier", supplierName);
		}

		// Define the column order for better readability
		List<string> columnOrder = [
			nameof(PurchaseOverviewModel.BillNo),
			nameof(PurchaseOverviewModel.BillDate),
			nameof(PurchaseOverviewModel.SupplierName),
			nameof(PurchaseOverviewModel.UserName),
			nameof(PurchaseOverviewModel.TotalItems),
			nameof(PurchaseOverviewModel.TotalQuantity),
			nameof(PurchaseOverviewModel.BaseTotal),
			nameof(PurchaseOverviewModel.DiscountAmount),
			nameof(PurchaseOverviewModel.TotalTaxAmount),
			nameof(PurchaseOverviewModel.SubTotal),
			nameof(PurchaseOverviewModel.CashDiscountAmount),
			nameof(PurchaseOverviewModel.Total),
			nameof(PurchaseOverviewModel.Remarks)
		];

		// Define custom column settings
		Dictionary<string, ExcelExportUtil.ColumnSetting> columnSettings = new()
		{
			[nameof(PurchaseOverviewModel.BillNo)] = new()
			{
				DisplayName = "Invoice No",
				Width = 15,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(PurchaseOverviewModel.BillDate)] = new()
			{
				DisplayName = "Date",
				Format = "dd-MMM-yyyy",
				Width = 15
			},
			[nameof(PurchaseOverviewModel.SupplierName)] = new()
			{
				DisplayName = "Supplier",
				Width = 25,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(PurchaseOverviewModel.UserName)] = new()
			{
				DisplayName = "Created By",
				Width = 20,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(PurchaseOverviewModel.TotalItems)] = new()
			{
				DisplayName = "Items",
				Format = "#,##0",
				Width = 12,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(PurchaseOverviewModel.TotalQuantity)] = new()
			{
				DisplayName = "Quantity",
				Format = "#,##0.00",
				Width = 15,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(PurchaseOverviewModel.BaseTotal)] = new()
			{
				DisplayName = "Base Total",
				Format = "₹#,##0.00",
				Width = 15,
				IsCurrency = true,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(PurchaseOverviewModel.DiscountAmount)] = new()
			{
				DisplayName = "Discount",
				Format = "₹#,##0.00",
				Width = 15,
				IsCurrency = true,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(PurchaseOverviewModel.TotalTaxAmount)] = new()
			{
				DisplayName = "Tax",
				Format = "₹#,##0.00",
				Width = 15,
				IsCurrency = true,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(PurchaseOverviewModel.SubTotal)] = new()
			{
				DisplayName = "Sub Total",
				Format = "₹#,##0.00",
				Width = 15,
				IsCurrency = true,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(PurchaseOverviewModel.CashDiscountAmount)] = new()
			{
				DisplayName = "Cash Discount",
				Format = "₹#,##0.00",
				Width = 15,
				IsCurrency = true,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(PurchaseOverviewModel.Total)] = new()
			{
				DisplayName = "Total",
				Format = "₹#,##0.00",
				Width = 15,
				IsCurrency = true,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight,
				FormatCallback = (value) =>
				{
					if (value == null) return null;
					var amount = Convert.ToDecimal(value);
					return new ExcelExportUtil.FormatInfo
					{
						Bold = amount > 10000,
						FontColor = amount > 50000 ? Syncfusion.Drawing.Color.FromArgb(56, 142, 60) : null
					};
				}
			},
			[nameof(PurchaseOverviewModel.Remarks)] = new()
			{
				DisplayName = "Remarks",
				Width = 30,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			}
		};

		// Generate the Excel file
		string reportTitle = "Purchase Report";
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

		var fileName = $"Purchase_Report_{_startDate:yyyy-MM-dd}_to_{_endDate:yyyy-MM-dd}.xlsx";
		await JS.InvokeVoidAsync("saveAs", Convert.ToBase64String(memoryStream.ToArray()), fileName);
	}

	// Chart data methods
	private List<DailyPurchaseData> GetDailyPurchaseData()
	{
		var result = new List<DailyPurchaseData>();
		if (_purchaseOverviews == null || _purchaseOverviews.Count == 0)
			return result;

		var groupedByDate = _purchaseOverviews
			.GroupBy(p => p.BillDate)
			.OrderBy(g => g.Key)
			.ToList();

		foreach (var group in groupedByDate)
		{
			result.Add(new DailyPurchaseData
			{
				Date = group.Key.ToString("dd/MM/yyyy"),
				Amount = group.Sum(p => p.Total)
			});
		}

		return result;
	}

	private List<VendorDistributionData> GetVendorDistributionData()
	{
		var result = new List<VendorDistributionData>();
		if (_purchaseOverviews == null || _purchaseOverviews.Count == 0)
			return result;

		var groupedBySupplier = _purchaseOverviews
			.GroupBy(p => new { p.SupplierId, p.SupplierName })
			.Select(g => new VendorDistributionData
			{
				SupplierId = g.Key.SupplierId,
				SupplierName = g.Key.SupplierName,
				Amount = g.Sum(p => p.Total)
			})
			.OrderByDescending(v => v.Amount)
			.Take(10) // Top 10 suppliers
			.ToList();

		return groupedBySupplier;
	}

	// Chart data classes
	public class DailyPurchaseData
	{
		public string Date { get; set; }
		public decimal Amount { get; set; }
	}

	public class VendorDistributionData
	{
		public int SupplierId { get; set; }
		public string SupplierName { get; set; }
		public decimal Amount { get; set; }
	}
}