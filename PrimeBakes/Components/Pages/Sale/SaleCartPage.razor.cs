using Microsoft.AspNetCore.Components;

#if ANDROID
using Plugin.LocalNotification;
#endif

using PrimeBakes.Services;

using PrimeOrdersLibrary.Data.Common;
using PrimeOrdersLibrary.Data.Sale;
using PrimeOrdersLibrary.DataAccess;
using PrimeOrdersLibrary.Exporting.Sale;
using PrimeOrdersLibrary.Models.Accounts.Masters;
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

	private decimal _baseTotal = 0;
	private decimal _discountAmount = 0;
	private decimal _subTotal = 0;
	private decimal _total = 0;
	private int _selectedPaymentModeId = 1;

	private bool _saleDetailsDialogVisible = false;
	private bool _saleConfirmationDialogVisible = false;
	private bool _validationErrorDialogVisible = false;
	private bool _discountDialogVisible = false;
	private bool _customerDialogVisible = false;

	private List<PaymentModeModel> _paymentModes = [];
	private readonly List<SaleProductCartModel> _cart = [];
	private readonly List<ValidationError> _validationErrors = [];

	private LocationModel _userLocation;
	private CustomerModel _customer = new();
	private SaleModel _sale = new()
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
		CreatedAt = DateTime.Now,
		Status = true,
	};

	public class ValidationError
	{
		public string Field { get; set; } = string.Empty;
		public string Message { get; set; } = string.Empty;
	}

	private SfGrid<SaleProductCartModel> _sfGrid;
	private SfDialog _sfSaleDetailsDialog;
	private SfDialog _sfSaleConfirmationDialog;
	private SfDialog _sfValidationErrorDialog;
	private SfDialog _sfDiscountDialog;
	private SfDialog _sfCustomerDialog;

	#region Load Data
	protected override async Task OnInitializedAsync()
	{
		_user = await AuthService.AuthenticateCurrentUser(NavManager);

		await LoadData();
		_isLoading = false;
	}

	private async Task LoadData()
	{
		_userLocation = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, _user.LocationId);

		_paymentModes = PaymentModeData.GetPaymentModes();
		_selectedPaymentModeId = _paymentModes.FirstOrDefault()?.Id ?? 1;

		await LoadSale();
		await LoadCustomer();
		await LoadCart();

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		StateHasChanged();
	}

	private async Task LoadSale()
	{
		_sale = System.Text.Json.JsonSerializer.Deserialize<SaleModel>(
					await File.ReadAllTextAsync(Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.Sale)));

		_sale.LocationId = _user.LocationId;
		_sale.UserId = _user.Id;
		_sale.BillNo = await GenerateCodes.GenerateSaleBillNo(_sale);
	}

	private async Task LoadCustomer()
	{
		if (_sale.CustomerId is not null && _sale.CustomerId > 0)
		{
			var existingCustomer = await CommonData.LoadTableDataById<CustomerModel>(TableNames.Customer, _sale.CustomerId.Value);
			if (existingCustomer is not null && existingCustomer.Id > 0)
				_customer = existingCustomer;
			else
			{
				_customer = new();
				_sale.CustomerId = null;
			}
		}
		else
		{
			_customer = new();
			_sale.CustomerId = null;
		}
	}

	private async Task LoadCart()
	{
		_cart.Clear();
		var fullPath = Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.SaleCart);
		if (File.Exists(fullPath))
		{
			var items = System.Text.Json.JsonSerializer.Deserialize<List<SaleProductCartModel>>(await File.ReadAllTextAsync(fullPath)) ?? [];
			foreach (var item in items)
				_cart.Add(item);
		}

		_cart.Sort((x, y) => string.Compare(x.ProductName, y.ProductName, StringComparison.Ordinal));

		UpdateFinancialDetails();
	}
	#endregion

	#region Customer Methods
	private async Task OnCustomerNumberChanged(string phoneNumber)
	{
		_customer.Number = phoneNumber;

		// Remove non-digit characters
		for (int i = 0; i < _customer.Number.Length; i++)
			if (!char.IsDigit(_customer.Number[i]))
			{
				_customer.Number = _customer.Number.Remove(i, 1);
				i--;
			}

		if (string.IsNullOrEmpty(_customer.Number))
		{
			_customer = new();
			_sale.CustomerId = null;
			StateHasChanged();
			return;
		}

		if (_customer.Number.Length > 10)
			_customer.Number = _customer.Number[..10];

		// Try to find existing customer
		var existingCustomer = await CustomerData.LoadCustomerByNumber(_customer.Number);
		if (existingCustomer is not null && existingCustomer.Id > 0)
		{
			_customer = existingCustomer;
			_sale.CustomerId = existingCustomer.Id;
		}
		else
		{
			_customer = new() { Number = _customer.Number };
			_sale.CustomerId = null;
		}

		await SaveSaleFile();
	}

	private async Task SaveCustomer()
	{
		if (!string.IsNullOrEmpty(_customer.Number) && !string.IsNullOrEmpty(_customer.Name))
		{
			// Validate and clean phone number
			for (int i = 0; i < _customer.Number.Length; i++)
				if (!char.IsDigit(_customer.Number[i]))
				{
					_customer.Number = _customer.Number.Remove(i, 1);
					i--;
				}

			if (_customer.Number.Length > 10)
				_customer.Number = _customer.Number[..10];

			// Check if customer already exists
			var existingCustomer = await CustomerData.LoadCustomerByNumber(_customer.Number);
			if (existingCustomer is not null && existingCustomer.Id > 0)
				_customer.Id = existingCustomer.Id;

			// Save or update customer
			_customer.Id = await CustomerData.InsertCustomer(_customer);
			if (_customer.Id > 0)
				_sale.CustomerId = _customer.Id;
		}

		_customerDialogVisible = false;
		await SaveSaleFile();
	}

	private async Task ClearCustomer()
	{
		_customer = new();
		_sale.CustomerId = null;
		await SaveSaleFile();
	}
	#endregion

	#region Products
	private async Task UpdateQuantity(SaleProductCartModel item, decimal newQuantity)
	{
		if (item is null || _isSaving)
			return;

		item.Quantity = Math.Max(0, newQuantity);

		if (item.Quantity == 0)
			_cart.Remove(item);

		await SaveSaleFile();
	}

	private async Task ClearCart()
	{
		if (_isSaving)
			return;

		_cart.Clear();

		if (Vibration.Default.IsSupported)
			Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500));

		_sale.OrderId = null;
		_sale.PartyId = null;

		await SaveSaleFile();
	}

	private async Task ApplyDiscount()
	{
		foreach (var item in _cart)
			item.DiscPercent = _sale.DiscPercent;

		_discountDialogVisible = false;
		await SaveSaleFile();
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

		_baseTotal = _cart.Sum(c => c.BaseTotal);
		_subTotal = _cart.Sum(c => c.AfterDiscount);
		_discountAmount = _baseTotal - _subTotal;
		_total = _cart.Sum(c => c.Total);

		switch (_selectedPaymentModeId)
		{
			case 1:
				_sale.Cash = _total;
				_sale.Card = 0;
				_sale.UPI = 0;
				_sale.Credit = 0;
				break;
			case 2:
				_sale.Cash = 0;
				_sale.Card = _total;
				_sale.UPI = 0;
				_sale.Credit = 0;
				break;
			case 3:
				_sale.Cash = 0;
				_sale.Card = 0;
				_sale.UPI = _total;
				_sale.Credit = 0;
				break;
			case 4:
				_sale.Cash = 0;
				_sale.Card = 0;
				_sale.UPI = 0;
				_sale.Credit = _total;
				break;
			default:
				_sale.Cash = _total;
				_sale.Card = 0;
				_sale.UPI = 0;
				_sale.Credit = 0;
				break;
		}

		_sfGrid?.Refresh();
		StateHasChanged();
	}
	#endregion

	#region Saving
	private async Task SaveSaleFile()
	{
		UpdateFinancialDetails();

		await File.WriteAllTextAsync(Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.Sale), System.Text.Json.JsonSerializer.Serialize(_sale));

		if (!_cart.Any(x => x.Quantity > 0) && File.Exists(Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.SaleCart)))
			File.Delete(Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.SaleCart));
		else
			await File.WriteAllTextAsync(Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.SaleCart), System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));

		HapticFeedback.Default.Perform(HapticFeedbackType.Click);

		await _sfGrid.Refresh();
		StateHasChanged();
	}

	private async Task<bool> ValidateForm()
	{
		await SaveSaleFile();

		_validationErrors.Clear();

		if (!_cart.Any(x => x.Quantity > 0))
		{
			_validationErrors.Add(new()
			{
				Field = "Cart",
				Message = "Cart is empty. Please add products to the cart."
			});
			return false;
		}

		var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, _sale.LocationId);
		if (location.MainLocation && _sale.Credit > 0 && _sale.PartyId is null)
		{
			_validationErrors.Add(new()
			{
				Field = "Party",
				Message = "Party is required when Payment Mode is Credit."
			});
			return false;
		}

		if (_sale.PartyId is not null && _sale.PartyId > 0)
		{
			var party = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, _sale.PartyId.Value);

			if (party is null || !party.Status)
			{
				_validationErrors.Add(new()
				{
					Field = "Party",
					Message = "Please select a valid party for the order."
				});
				return false;
			}

			if (_sale.PartyId is not null && _sale.LocationId > 0)
			{
				var partyLocation = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, party.LocationId.Value);

				if (partyLocation is null || !partyLocation.Status || party.LocationId <= 1)
				{
					_validationErrors.Add(new()
					{
						Field = "Party",
						Message = "The selected party does not have a valid location assigned. Please contact the administrator."
					});
					return false;
				}
			}
		}

		if (_sale.DiscPercent < 0 || _sale.DiscPercent > 100)
		{
			_validationErrors.Add(new()
			{
				Field = "Discount",
				Message = "Discount percent must be between 0 and 100."
			});
			return false;
		}

		if (_sale.Cash < 0 || _sale.Card < 0 || _sale.UPI < 0 || _sale.Credit < 0)
		{
			_validationErrors.Add(new()
			{
				Field = "Payment",
				Message = "Payment amounts cannot be negative."
			});
			return false;
		}

		if (_sale.Cash + _sale.Card + _sale.UPI + _sale.Credit != _total)
		{
			_validationErrors.Add(new()
			{
				Field = "Payment",
				Message = "Total payment amount must equal the total sale amount."
			});
			return false;
		}

		return true;
	}

	private async Task SaveSale()
	{
		if (_isSaving)
			return;

		_isSaving = true;
		StateHasChanged();

		try
		{
			if (!await ValidateForm())
			{
				_validationErrorDialogVisible = true;
				_saleConfirmationDialogVisible = false;
				return;
			}

			await InsertCustomer();
			_sale.Id = await SaleData.SaveSale(_sale, _cart);
			await PrintSaleBill();
			DeleteCartSale();

#if ANDROID
			await CreateNotification();
#endif

			NavManager.NavigateTo("/Sale/Confirmed", true);
		}
		catch (Exception ex)
		{
			_validationErrors.Add(new()
			{
				Field = "Exception",
				Message = $"An error occurred while saving the Sale: {ex.Message}"
			});
			_validationErrorDialogVisible = true;
			_saleConfirmationDialogVisible = false;
		}
		finally
		{
			_isSaving = false;
			StateHasChanged();
		}
	}

	private async Task InsertCustomer()
	{
		if (string.IsNullOrEmpty(_customer.Number) || string.IsNullOrEmpty(_customer.Name))
			return;

		for (int i = 0; i < _customer.Number.Length; i++)
			if (!char.IsDigit(_customer.Number[i]))
			{
				_customer.Number = _customer.Number.Remove(i, 1);
				i--;
			}

		if (_customer.Number.Length > 10)
			_customer.Number = _customer.Number[..10];

		var existingCustomer = await CustomerData.LoadCustomerByNumber(_customer.Number);
		if (existingCustomer is not null && existingCustomer.Id > 0)
			_customer.Id = existingCustomer.Id;

		_customer.Id = await CustomerData.InsertCustomer(_customer);
		if (_customer.Id > 0)
			_sale.CustomerId = _customer.Id;
	}

	private async Task PrintSaleBill()
	{
		var memoryStream = await SaleA4Print.GenerateA4SaleBill(_sale.Id);
		var fileName = $"Sale_Bill_{_sale.BillNo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
		SaveService saveService = new();
		var filePath = saveService.SaveAndView(fileName, "application/pdf", memoryStream);
	}

	private void DeleteCartSale()
	{
		_cart.Clear();

		File.Delete(Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.SaleCart));
		File.Delete(Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.Sale));
	}

#if ANDROID
	private async Task CreateNotification()
	{
		if (await LocalNotificationCenter.Current.AreNotificationsEnabled() == false)
			await LocalNotificationCenter.Current.RequestNotificationPermission();

		var request = new NotificationRequest
		{
			NotificationId = _sale.Id,
			Title = "Sale Placed",
			Subtitle = "Sale Confirmation",
			Description = $"Your sale #{_sale.BillNo} has been successfully placed. {_sale.Remarks}",
			Schedule = new NotificationRequestSchedule
			{
				NotifyTime = DateTime.Now.AddSeconds(5)
			}
		};

		await LocalNotificationCenter.Current.Show(request);
	}
#endif
	#endregion
}