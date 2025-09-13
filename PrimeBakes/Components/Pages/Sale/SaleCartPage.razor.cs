using Microsoft.AspNetCore.Components;

using PrimeBakes.Services;

using PrimeOrdersLibrary.Data.Common;
using PrimeOrdersLibrary.DataAccess;
using PrimeOrdersLibrary.Models.Common;
using PrimeOrdersLibrary.Models.Sale;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Components.Pages.Sale;

public partial class SaleCartPage
{
	[Inject] public NavigationManager NavManager { get; set; }

	private UserModel _user;
	private bool _isLoading = true;
	private bool _isSaving = false;

	private bool _saleDetailsDialogVisible = false;
	private bool _saleConfirmationDialogVisible = false;
	private bool _validationErrorDialogVisible = false;

	private List<LocationModel> _locations = [];
	private readonly List<SaleProductCartModel> _cart = [];

	private LocationModel _userLocation;
	private readonly SaleModel _sale = new()
	{
		Id = 0,
		SaleDateTime = DateTime.Now,
		OrderId = null,
		Remarks = "",
		Cash = 0,
		Card = 0,
		UPI = 0,
		Credit = 0,
		PartyId = null,
		CustomerId = null,
		DiscPercent = 0,
		DiscReason = "",
		Status = true,
	};
	private readonly List<ValidationError> _validationErrors = [];

	// Validation Error Model
	public class ValidationError
	{
		public string Field { get; set; } = string.Empty;
		public string Message { get; set; } = string.Empty;
	}

	private SfGrid<SaleProductCartModel> _sfGrid;
	private SfDialog _sfSaleDetailsDialog;
	private SfDialog _sfSaleConfirmationDialog;
	private SfDialog _sfValidationErrorDialog;

	protected override async Task OnInitializedAsync()
	{
		_user = await AuthService.AuthenticateCurrentUser(NavManager);

		await LoadData();
		_isLoading = false;
	}

	private async Task LoadData()
	{
		_locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location);
		_userLocation = _locations.FirstOrDefault(c => c.Id == _user.LocationId);
		_locations.RemoveAll(c => c.MainLocation);

		_sale.LocationId = _user.LocationId == 1 ? _locations.FirstOrDefault().Id : _user.LocationId;
		_sale.BillNo = await GenerateCodes.GenerateSaleBillNo(_sale);
		_sale.UserId = _user.Id;

		_cart.Clear();
		var fullPath = Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.SaleCart);
		if (File.Exists(fullPath))
		{
			var items = System.Text.Json.JsonSerializer.Deserialize<List<SaleProductCartModel>>(await File.ReadAllTextAsync(fullPath)) ?? [];
			foreach (var item in items)
				_cart.Add(item);
		}

		_cart.Sort((x, y) => string.Compare(x.ProductName, y.ProductName, StringComparison.Ordinal));

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		StateHasChanged();
	}

	private async Task UpdateQuantity(SaleProductCartModel item, decimal newQuantity)
	{
		if (item is null || _isSaving)
			return;

		item.Quantity = Math.Max(0, newQuantity);

		if (item.Quantity == 0)
			_cart.Remove(item);

		UpdateFinancialDetails();

		await _sfGrid.Refresh();
		StateHasChanged();

		if (_cart.Count == 0 && File.Exists(Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.SaleCart)))
			File.Delete(Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.SaleCart));
		else
			await File.WriteAllTextAsync(Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.SaleCart), System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));

		HapticFeedback.Default.Perform(HapticFeedbackType.Click);
	}

	private async Task ClearCart()
	{
		if (_isSaving)
			return;

		_cart.Clear();
		UpdateFinancialDetails();
		File.Delete(Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.SaleCart));

		if (Vibration.Default.IsSupported)
			Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500));

		await _sfGrid.Refresh();
		StateHasChanged();
	}

	private void UpdateFinancialDetails()
	{
		foreach (var item in _cart)
		{
			item.BaseTotal = item.Rate * item.Quantity;
			item.DiscAmount = item.BaseTotal * (item.DiscPercent / 100);
			item.AfterDiscount = item.BaseTotal - item.DiscAmount;
			item.CGSTAmount = item.AfterDiscount * (item.CGSTPercent / 100);
			item.SGSTAmount = item.AfterDiscount * (item.SGSTPercent / 100);
			item.IGSTAmount = item.AfterDiscount * (item.IGSTPercent / 100);
			item.Total = item.AfterDiscount + item.CGSTAmount + item.SGSTAmount + item.IGSTAmount;
			item.NetRate = item.Quantity > 0 ? item.Total / item.Quantity : 0;
		}

		_sfGrid?.Refresh();
		StateHasChanged();
	}

	private async Task SaveSale()
	{

	}
}