using PrimeOrdersLibrary.Exporting.Purchase;
using PrimeOrdersLibrary.Models.Accounts.Masters;

using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.Grids;

namespace PrimeOrders.Components.Pages.Reports.Purchase;

public partial class PurchaseReport
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] private IJSRuntime JS { get; set; }

	private UserModel _user;
	private bool _isLoading = true;
	private bool _purchaseSummaryVisible = false;

	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30));
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);

	private List<LedgerModel> _suppliers = [];
	private List<PurchaseOverviewModel> _purchaseOverviews = [];
	private LedgerModel _selectedSupplier = new();

	private PurchaseOverviewModel _selectedPurchase;
	private readonly List<PurchaseDetailDisplayModel> _selectedPurchaseDetails = [];

	private SfGrid<PurchaseOverviewModel> _sfGrid;

	#region Load Data
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
		_suppliers = await CommonData.LoadTableDataByStatus<LedgerModel>(TableNames.Ledger, true);
		_suppliers.Insert(0, new LedgerModel { Id = 0, Name = "All Suppliers" });
		_selectedSupplier = _suppliers.FirstOrDefault();
	}

	private async Task DateRangeChanged(RangePickerEventArgs<DateOnly> args)
	{
		_startDate = args.StartDate;
		_endDate = args.EndDate;
		await LoadData();
	}

	private async Task OnSupplierChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<LedgerModel, LedgerModel> args)
	{
		if (args.ItemData is null)
			_selectedSupplier = _suppliers.FirstOrDefault();

		else
			_selectedSupplier = args.ItemData;

		await LoadData();
	}

	private async Task LoadData()
	{
		var allPurchases = await PurchaseData.LoadPurchaseDetailsByDate(
			_startDate.ToDateTime(new TimeOnly(0, 0)),
			_endDate.ToDateTime(new TimeOnly(23, 59)));

		if (_selectedSupplier.Id > 0)
			_purchaseOverviews = [.. allPurchases.Where(p => p.SupplierId == _selectedSupplier.Id)];
		else
			_purchaseOverviews = allPurchases;

		StateHasChanged();
	}
	#endregion

	#region Purchase Summary Module Methods
	private async Task OnPurchaseRowSelected(RowSelectEventArgs<PurchaseOverviewModel> args)
	{
		_selectedPurchase = args.Data;
		await LoadPurchaseDetails(_selectedPurchase.PurchaseId);
		_purchaseSummaryVisible = true;
		StateHasChanged();
	}

	private async Task LoadPurchaseDetails(int purchaseId)
	{
		_selectedPurchaseDetails.Clear();

		var purchaseDetails = await PurchaseData.LoadPurchaseDetailByPurchase(purchaseId);
		foreach (var detail in purchaseDetails)
		{
			var rawMaterial = await CommonData.LoadTableDataById<RawMaterialModel>(TableNames.RawMaterial, detail.RawMaterialId);
			if (rawMaterial is not null)
				_selectedPurchaseDetails.Add(new()
				{
					RawMaterialName = rawMaterial.Name,
					Quantity = detail.Quantity,
					Rate = detail.Rate,
					Total = detail.Total
				});
		}
	}

	private async Task OnPurchaseSummaryVisibilityChanged(bool isVisible)
	{
		_purchaseSummaryVisible = isVisible;
		await LoadData();
		StateHasChanged();
	}
	#endregion

	#region Excel Export
	private async Task ExportToExcel()
	{
		if (_purchaseOverviews is null || _purchaseOverviews.Count == 0)
		{
			await JS.InvokeVoidAsync("alert", "No data to export");
			return;
		}

		var memoryStream = await PurchaseExcelExport.ExportPurchaseOverviewExcel(_purchaseOverviews, _startDate, _endDate, _selectedSupplier.Id);
		var fileName = $"Purchase_Report_{_startDate:yyyy-MM-dd}_to_{_endDate:yyyy-MM-dd}.xlsx";
		await JS.InvokeVoidAsync("saveExcel", Convert.ToBase64String(memoryStream.ToArray()), fileName);
	}
	#endregion

	#region Chart Data
	private List<DailyPurchaseChartData> GetDailyPurchaseData()
	{
		var result = new List<DailyPurchaseChartData>();
		if (_purchaseOverviews == null || _purchaseOverviews.Count == 0)
			return result;

		var groupedByDate = _purchaseOverviews
			.GroupBy(p => p.BillDateTime.Date)
			.OrderBy(g => g.Key)
			.ToList();

		foreach (var group in groupedByDate)
		{
			result.Add(new DailyPurchaseChartData
			{
				Date = group.Key.ToString("dd/MM/yyyy"),
				Amount = group.Sum(p => p.Total)
			});
		}

		return result;
	}

	private List<VendorDistributionChartData> GetVendorDistributionData()
	{
		var result = new List<VendorDistributionChartData>();
		if (_purchaseOverviews == null || _purchaseOverviews.Count == 0)
			return result;

		var groupedBySupplier = _purchaseOverviews
			.GroupBy(p => new { p.SupplierId, p.SupplierName })
			.Select(g => new VendorDistributionChartData
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
	#endregion
}