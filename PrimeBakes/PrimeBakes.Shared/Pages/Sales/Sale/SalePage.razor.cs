using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakesLibrary.Data;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Sales.Order;
using PrimeBakesLibrary.Data.Sales.Product;
using PrimeBakesLibrary.Data.Sales.Sale;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sales.Order;
using PrimeBakesLibrary.Models.Sales.Product;
using PrimeBakesLibrary.Models.Sales.Sale;

using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Inputs;

namespace PrimeBakes.Shared.Pages.Sales.Sale;

public partial class SalePage : IAsyncDisposable
{
	private HotKeysContext _hotKeysContext;

	[Parameter] public int? Id { get; set; }

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;

	private CompanyModel _selectedCompany = new();
	private LocationModel _selectedLocation = new();
	private LedgerModel? _selectedParty = new();
	private CustomerModel _selectedCustomer = new();
	private OrderModel? _selectedOrder = new();
	private FinancialYearModel _selectedFinancialYear = new();
	private ProductLocationOverviewModel? _selectedProduct = new();
	private SaleItemCartModel _selectedCart = new();
	private SaleModel _sale = new();

	private List<CompanyModel> _companies = [];
	private List<LocationModel> _locations = [];
	private List<LedgerModel> _parties = [];
	private List<OrderModel> _orders = [];
	private List<ProductLocationOverviewModel> _products = [];
	private List<TaxModel> _taxes = [];
	private List<SaleItemCartModel> _cart = [];

	private SfAutoComplete<ProductLocationOverviewModel?, ProductLocationOverviewModel> _sfItemAutoComplete;
	private SfGrid<SaleItemCartModel> _sfCartGrid;

	ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Sales);
		await LoadData();
		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		_hotKeysContext = HotKeys.CreateContext()
			.Add(ModCode.Ctrl, Code.Enter, AddItemToCart, "Add item to cart", Exclude.None)
			.Add(ModCode.Ctrl, Code.E, () => _sfItemAutoComplete.FocusAsync(), "Focus on item input", Exclude.None)
			.Add(ModCode.Ctrl, Code.S, SaveTransaction, "Save the transaction", Exclude.None)
			.Add(ModCode.Alt, Code.P, DownloadPdfInvoice, "Download PDF invoice", Exclude.None)
			.Add(ModCode.Alt, Code.E, DownloadExcelInvoice, "Download Excel invoice", Exclude.None)
			.Add(ModCode.Ctrl, Code.H, NavigateToTransactionHistoryPage, "Open transaction history", Exclude.None)
			.Add(ModCode.Ctrl, Code.I, NavigateToItemReport, "Open item report", Exclude.None)
			.Add(ModCode.Ctrl, Code.N, ResetPage, "Reset the page", Exclude.None)
			.Add(ModCode.Ctrl, Code.D, NavigateToDashboard, "Go to dashboard", Exclude.None)
			.Add(ModCode.Ctrl, Code.B, NavigateBack, "Back", Exclude.None)
			.Add(Code.Delete, RemoveSelectedCartItem, "Delete selected cart item", Exclude.None)
			.Add(Code.Insert, EditSelectedCartItem, "Edit selected cart item", Exclude.None);

		await LoadLocations();
		await LoadCompanies();
		await LoadLedgers();
		await LoadExistingTransaction();
		await LoadItems();
		await LoadExistingCart();
		await SaveTransactionFile();
	}

	private async Task LoadLocations()
	{
		try
		{
			_locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location);
			_locations = [.. _locations.OrderBy(s => s.Name)];
			_locations.Add(new()
			{
				Id = 0,
				Name = "Create New Location ..."
			});

			_selectedLocation = _locations.FirstOrDefault(s => s.Id == _user.LocationId);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Locations", ex.Message, ToastType.Error);
		}
	}

	private async Task LoadCompanies()
	{
		try
		{
			_companies = await CommonData.LoadTableDataByStatus<CompanyModel>(TableNames.Company);
			_companies = [.. _companies.OrderBy(s => s.Name)];
			_companies.Add(new()
			{
				Id = 0,
				Name = "Create New Company ..."
			});

			var mainCompanyId = await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId);
			_selectedCompany = _companies.FirstOrDefault(s => s.Id.ToString() == mainCompanyId.Value) ?? throw new Exception("Main Company Not Found");
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Companies", ex.Message, ToastType.Error);
		}
	}

	private async Task LoadLedgers()
	{
		try
		{
			_parties = await CommonData.LoadTableDataByStatus<LedgerModel>(TableNames.Ledger);
			_parties = [.. _parties.OrderBy(s => s.Name)];
			_parties.Add(new()
			{
				Id = 0,
				Name = "Create New Party Ledger..."
			});

			_selectedParty = null;
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Ledgers", ex.Message, ToastType.Error);
		}
	}

	private async Task LoadExistingTransaction()
	{
		try
		{
			if (Id.HasValue)
			{
				_sale = await CommonData.LoadTableDataById<SaleModel>(TableNames.Sale, Id.Value);
				if (_sale is null || _sale.Id == 0 || _user.LocationId > 1)
				{
					await _toastNotification.ShowAsync("Transaction Not Found", "The requested transaction could not be found.", ToastType.Error);
					NavigationManager.NavigateTo(PageRouteNames.Sale, true);
				}
			}

			else if (await DataStorageService.LocalExists(StorageFileNames.SaleDataFileName))
				_sale = System.Text.Json.JsonSerializer.Deserialize<SaleModel>(await DataStorageService.LocalGetAsync(StorageFileNames.SaleDataFileName));

			else
			{
				_sale = new()
				{
					Id = 0,
					TransactionNo = string.Empty,
					CompanyId = _selectedCompany.Id,
					LocationId = _user.LocationId,
					PartyId = null,
					CustomerId = null,
					OrderId = null,
					TransactionDateTime = await CommonData.LoadCurrentDateTime(),
					FinancialYearId = (await FinancialYearData.LoadFinancialYearByDateTime(await CommonData.LoadCurrentDateTime())).Id,
					CreatedBy = _user.Id,
					TotalItems = 0,
					ItemDiscountAmount = 0,
					TotalAfterItemDiscount = 0,
					TotalInclusiveTaxAmount = 0,
					TotalExtraTaxAmount = 0,
					TotalAfterTax = 0,
					DiscountPercent = 0,
					DiscountAmount = 0,
					OtherChargesPercent = 0,
					OtherChargesAmount = 0,
					RoundOffAmount = 0,
					TotalAmount = 0,
					Card = 0,
					Cash = 0,
					Credit = 0,
					UPI = 0,
					Remarks = "",
					CreatedAt = DateTime.Now,
					CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform(),
					Status = true,
					LastModifiedAt = null,
					LastModifiedBy = null,
					LastModifiedFromPlatform = null
				};
				await DeleteLocalFiles();
			}

			if (_user.LocationId == 1)
				_selectedLocation = _locations.FirstOrDefault(s => s.Id == _sale.LocationId);
			else
			{
				_selectedLocation = _locations.FirstOrDefault(s => s.Id == _user.LocationId);
				_sale.LocationId = _selectedLocation.Id;

				_sale.TransactionDateTime = await CommonData.LoadCurrentDateTime();
			}

			if (_sale.CompanyId > 0 && _user.LocationId == 1)
				_selectedCompany = _companies.FirstOrDefault(s => s.Id == _sale.CompanyId);
			else
			{
				var mainCompanyId = await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId);
				_selectedCompany = _companies.FirstOrDefault(s => s.Id.ToString() == mainCompanyId.Value);
				_sale.CompanyId = _selectedCompany.Id;
			}

			if (_sale.PartyId is not null && _sale.LocationId == 1 && _sale.PartyId > 0)
				_selectedParty = _parties.FirstOrDefault(s => s.Id == _sale.PartyId);
			else
			{
				_selectedParty = null;
				_sale.PartyId = null;
			}

			if (_sale.CustomerId is not null && _sale.CustomerId > 0)
				_selectedCustomer = await CommonData.LoadTableDataById<CustomerModel>(TableNames.Customer, _sale.CustomerId.Value);
			else
			{
				_selectedCustomer = new();
				_sale.CustomerId = null;
			}

			if (_selectedParty is not null && _selectedParty.LocationId is not null && _selectedParty.LocationId > 0)
			{
				if (Id is null)
				{
					var location = _locations.FirstOrDefault(s => s.Id == _selectedParty.LocationId.Value);
					_sale.DiscountPercent = location.Discount;
				}

				_orders = await OrderData.LoadOrderByLocationPending(_selectedParty.LocationId.Value);
				_orders = [.. _orders.OrderByDescending(s => s.TransactionDateTime)];

				if (_sale.OrderId is not null && _sale.OrderId > 0)
				{
					if (Id > 0)
					{
						var order = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, _sale.OrderId.Value);
						if (order is not null && _selectedParty.LocationId == order.LocationId)
						{
							if (_orders.FirstOrDefault(s => s.Id == order.Id) is null)
								_orders.Insert(0, order);
						}
					}

					_selectedOrder = _orders.FirstOrDefault(s => s.Id == _sale.OrderId);
				}

				else
				{
					_selectedOrder = null;
					_sale.OrderId = null;
				}
			}
			else
			{
				_selectedOrder = null;
				_sale.OrderId = null;
			}

			_selectedFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, _sale.FinancialYearId);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Transaction Data", ex.Message, ToastType.Error);
			await DeleteLocalFiles();
		}
		finally
		{
			await SaveTransactionFile();
		}
	}

	private async Task LoadItems()
	{
		try
		{
			_products = await ProductData.LoadProductByLocation(_sale.LocationId);
			_taxes = await CommonData.LoadTableDataByStatus<TaxModel>(TableNames.Tax);

			_products = [.. _products.OrderBy(s => s.Name)];

			if (_user.LocationId == 1)
				_products.Add(new()
				{
					Id = 0,
					Name = "Create New Item ..."
				});
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Items", ex.Message, ToastType.Error);
		}
	}

	private async Task LoadExistingCart()
	{
		try
		{
			_cart.Clear();

			if (_sale.Id > 0)
			{
				var existingCart = await CommonData.LoadTableDataByMasterId<SaleDetailModel>(TableNames.SaleDetail, _sale.Id);

				foreach (var item in existingCart)
				{
					if (_products.FirstOrDefault(s => s.ProductId == item.ProductId) is null)
					{
						var product = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, item.ProductId);
						await _toastNotification.ShowAsync("Item Not Found", $"The item {product?.Name} (ID: {item.ProductId}) in the existing transaction cart was not found in the available items list. It may have been deleted or is inaccessible.", ToastType.Error);
						continue;
					}

					_cart.Add(new()
					{
						ItemId = item.ProductId,
						ItemName = _products.FirstOrDefault(s => s.ProductId == item.ProductId)?.Name ?? "",
						Quantity = item.Quantity,
						Rate = item.Rate,
						BaseTotal = item.BaseTotal,
						DiscountPercent = item.DiscountPercent,
						DiscountAmount = item.DiscountAmount,
						AfterDiscount = item.AfterDiscount,
						CGSTPercent = item.CGSTPercent,
						CGSTAmount = item.CGSTAmount,
						SGSTPercent = item.SGSTPercent,
						SGSTAmount = item.SGSTAmount,
						IGSTPercent = item.IGSTPercent,
						IGSTAmount = item.IGSTAmount,
						TotalTaxAmount = item.TotalTaxAmount,
						Total = item.Total,
						InclusiveTax = item.InclusiveTax,
						NetRate = item.NetRate,
						Remarks = item.Remarks
					});
				}
			}

			else if (await DataStorageService.LocalExists(StorageFileNames.SaleCartDataFileName))
				_cart = System.Text.Json.JsonSerializer.Deserialize<List<SaleItemCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.SaleCartDataFileName));
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Existing Cart", ex.Message, ToastType.Error);
			await DeleteLocalFiles();
		}
		finally
		{
			await SaveTransactionFile();
		}
	}
	#endregion

	#region Change Events
	private async Task OnLocationChanged(ChangeEventArgs<LocationModel, LocationModel> args)
	{
		if (_user.LocationId > 1)
		{
			_selectedLocation = _locations.FirstOrDefault(s => s.Id == _user.LocationId);
			_sale.LocationId = _selectedLocation.Id;
			await _toastNotification.ShowAsync("Location Change Not Allowed", "You are not allowed to change the location.", ToastType.Error);
			return;
		}

		if (args.Value is null)
			return;

		if (args.Value.Id == 0)
		{
			if (FormFactor.GetFormFactor() == "Web")
				await JSRuntime.InvokeVoidAsync("open", PageRouteNames.AdminLocation, "_blank");
			else
				NavigationManager.NavigateTo(PageRouteNames.AdminLocation);

			return;
		}

		_selectedLocation = args.Value;
		_sale.LocationId = _selectedLocation.Id;

		if (_sale.LocationId > 1)
		{
			_selectedParty = null;
			_sale.PartyId = null;

			_orders.Clear();
			_cart.Clear();
			_selectedOrder = null;
			_sale.OrderId = null;
			await _toastNotification.ShowAsync("Party & Order Cleared", "The party & order has been cleared as the selected location is not the main location.", ToastType.Info);
		}

		await LoadItems();
		await SaveTransactionFile();
	}

	private async Task OnCompanyChanged(ChangeEventArgs<CompanyModel, CompanyModel> args)
	{
		if (_user.LocationId > 1)
		{
			var mainCompanyId = await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId);
			_selectedCompany = _companies.FirstOrDefault(s => s.Id.ToString() == mainCompanyId.Value);
			_sale.CompanyId = _selectedCompany.Id;
			await _toastNotification.ShowAsync("Company Change Not Allowed", "You are not allowed to change the company.", ToastType.Error);
			return;
		}

		if (args.Value is null)
			return;

		if (args.Value.Id == 0)
		{
			if (FormFactor.GetFormFactor() == "Web")
				await JSRuntime.InvokeVoidAsync("open", PageRouteNames.AdminCompany, "_blank");
			else
				NavigationManager.NavigateTo(PageRouteNames.AdminCompany);

			return;
		}

		_selectedCompany = args.Value;
		_sale.CompanyId = _selectedCompany.Id;

		await SaveTransactionFile();
	}

	private async Task OnPartyChanged(ChangeEventArgs<LedgerModel?, LedgerModel?> args)
	{
		if (_user.LocationId > 1 || _sale.LocationId > 1)
		{
			_selectedParty = null;
			_sale.PartyId = null;
			await _toastNotification.ShowAsync("Party Change Not Allowed", "You are not allowed to change the party.", ToastType.Error);
			return;
		}

		if (args.Value is null)
		{
			_selectedParty = null;
			_sale.PartyId = null;

			_orders.Clear();
			_selectedOrder = null;
			_sale.OrderId = null;

			return;
		}

		if (args.Value.Id == 0)
		{
			if (FormFactor.GetFormFactor() == "Web")
				await JSRuntime.InvokeVoidAsync("open", PageRouteNames.AdminLedger, "_blank");
			else
				NavigationManager.NavigateTo(PageRouteNames.AdminLedger);

			return;
		}

		_selectedParty = args.Value;
		_sale.PartyId = _selectedParty.Id;

		_orders.Clear();
		_selectedOrder = null;
		_sale.OrderId = null;

		if (_selectedParty.LocationId is not null && _selectedParty.LocationId > 0)
		{
			var location = _locations.FirstOrDefault(s => s.Id == _selectedParty.LocationId.Value);
			_sale.DiscountPercent = location.Discount;

			_orders = await OrderData.LoadOrderByLocationPending(_selectedParty.LocationId.Value);
			_orders = [.. _orders.OrderByDescending(s => s.TransactionDateTime)];

			if (Id > 0)
			{
				var order = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, _sale.OrderId.Value);
				if (order is not null && _selectedParty.LocationId == order.LocationId)
				{
					if (_orders.FirstOrDefault(s => s.Id == order.Id) is null)
						_orders.Insert(0, order);
				}
			}
		}

		await LoadItems();
		await SaveTransactionFile();
	}

	private async Task OnOrderChanged(ChangeEventArgs<OrderModel?, OrderModel?> args)
	{
		if (_user.LocationId > 1 || _sale.LocationId > 1)
		{
			_selectedOrder = null;
			_sale.OrderId = null;
			await _toastNotification.ShowAsync("Order Change Not Allowed", "You are not allowed to change the order.", ToastType.Error);
			return;
		}

		if (args.Value is null)
		{
			_selectedOrder = null;
			_sale.OrderId = null;
			return;
		}

		_selectedOrder = args.Value;
		_sale.OrderId = _selectedOrder.Id;
		_cart.Clear();

		var orderItems = await CommonData.LoadTableDataByMasterId<OrderDetailModel>(TableNames.OrderDetail, _selectedOrder.Id);
		foreach (var item in orderItems)
		{
			if (_products.FirstOrDefault(s => s.ProductId == item.ProductId) is null)
			{
				var product = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, item.ProductId);
				await _toastNotification.ShowAsync("Item Not Found", $"The item {product?.Name} (ID: {item.ProductId}) in the selected order was not found in the available items list. It may have been deleted or is inaccessible.", ToastType.Error);
				continue;
			}

			var isSameState = _selectedParty is null || _selectedParty.StateUTId == _selectedCompany.StateUTId;

			_cart.Add(new()
			{
				ItemId = item.ProductId,
				ItemName = _products.FirstOrDefault(s => s.ProductId == item.ProductId)?.Name ?? "",
				Quantity = item.Quantity,
				Rate = _products.FirstOrDefault(s => s.ProductId == item.ProductId)?.Rate ?? 0,
				DiscountPercent = 0,
				CGSTPercent = _taxes.FirstOrDefault(s => s.Id == _products.FirstOrDefault(p => p.ProductId == item.ProductId)?.TaxId).CGST,
				SGSTPercent = isSameState ? _taxes.FirstOrDefault(s => s.Id == _products.FirstOrDefault(p => p.ProductId == item.ProductId)?.TaxId).SGST : 0,
				IGSTPercent = isSameState ? 0 : _taxes.FirstOrDefault(s => s.Id == _products.FirstOrDefault(p => p.ProductId == item.ProductId)?.TaxId).IGST,
				InclusiveTax = _taxes.FirstOrDefault(s => s.Id == _products.FirstOrDefault(p => p.ProductId == item.ProductId)?.TaxId).Inclusive
			});
		}

		await SaveTransactionFile();
	}

	private async Task OnTransactionDateChanged(Syncfusion.Blazor.Calendars.ChangedEventArgs<DateTime> args)
	{
		if (_user.LocationId > 1)
		{
			_sale.TransactionDateTime = await CommonData.LoadCurrentDateTime();
			await _toastNotification.ShowAsync("Transaction Date Change Not Allowed", "You are not allowed to change the transaction date.", ToastType.Error);
			return;
		}

		_sale.TransactionDateTime = args.Value;
		await LoadItems();
		await SaveTransactionFile();
	}

	private async Task OnCustomerNumberChanged(string args)
	{
		if (string.IsNullOrWhiteSpace(args))
		{
			_selectedCustomer = new();
			_sale.CustomerId = null;
			await SaveTransactionFile();
			return;
		}

		args = args.Trim();
		_selectedCustomer = await CustomerData.LoadCustomerByNumber(args);
		_selectedCustomer ??= new()
		{
			Id = 0,
			Name = "",
			Number = args
		};

		_sale.CustomerId = _selectedCustomer.Id;
		await SaveTransactionFile();
	}

	private async Task OnDiscountPercentChanged(ChangeEventArgs<decimal> args)
	{
		_sale.DiscountPercent = args.Value;
		await SaveTransactionFile();
	}

	private async Task OnOtherDiscountPercentChanged(ChangeEventArgs<decimal> args)
	{
		_sale.OtherChargesPercent = args.Value;
		await SaveTransactionFile();
	}

	private async Task OnRoundOffAmountChanged(ChangeEventArgs<decimal> args)
	{
		_sale.RoundOffAmount = args.Value;
		await SaveTransactionFile(true);
	}
	#endregion

	#region Cart
	private async Task OnItemChanged(ChangeEventArgs<ProductLocationOverviewModel?, ProductLocationOverviewModel?> args)
	{
		if (args.Value is null)
			return;

		if (args.Value.Id == 0)
		{
			if (FormFactor.GetFormFactor() == "Web")
				await JSRuntime.InvokeVoidAsync("open", PageRouteNames.AdminProduct, "_blank");
			else
				NavigationManager.NavigateTo(PageRouteNames.AdminProduct);

			return;
		}

		_selectedProduct = args.Value;

		if (_selectedProduct is null)
			_selectedCart = new()
			{
				ItemId = 0,
				ItemName = "",
				Quantity = 1,
				Rate = 0,
				DiscountPercent = 0,
				CGSTPercent = 0,
				SGSTPercent = 0,
				IGSTPercent = 0
			};

		else
		{
			var isSameState = _selectedParty is null || _selectedParty.StateUTId == _selectedCompany.StateUTId;

			_selectedCart.ItemId = _selectedProduct.ProductId;
			_selectedCart.ItemName = _selectedProduct.Name;
			_selectedCart.Quantity = 1;
			_selectedCart.Rate = _selectedProduct.Rate;
			_selectedCart.DiscountPercent = 0;
			_selectedCart.CGSTPercent = _taxes.FirstOrDefault(s => s.Id == _selectedProduct.TaxId).CGST;
			_selectedCart.SGSTPercent = isSameState ? _taxes.FirstOrDefault(s => s.Id == _selectedProduct.TaxId).SGST : 0;
			_selectedCart.IGSTPercent = isSameState ? 0 : _taxes.FirstOrDefault(s => s.Id == _selectedProduct.TaxId).IGST;
			_selectedCart.InclusiveTax = _taxes.FirstOrDefault(s => s.Id == _selectedProduct.TaxId).Inclusive;
		}

		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemQuantityChanged(ChangeEventArgs<decimal> args)
	{
		_selectedCart.Quantity = args.Value;
		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemRateChanged(ChangeEventArgs<decimal> args)
	{
		_selectedCart.Rate = args.Value;
		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemDiscountPercentChanged(ChangeEventArgs<decimal> args)
	{
		_selectedCart.DiscountPercent = args.Value;
		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemCGSTPercentChanged(ChangeEventArgs<decimal> args)
	{
		_selectedCart.CGSTPercent = args.Value;
		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemSGSTPercentChanged(ChangeEventArgs<decimal> args)
	{
		_selectedCart.SGSTPercent = args.Value;
		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemIGSTPercentChanged(ChangeEventArgs<decimal> args)
	{
		_selectedCart.IGSTPercent = args.Value;
		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemInclusiveTaxChanged(Syncfusion.Blazor.Buttons.ChangeEventArgs<bool> args)
	{
		_selectedCart.InclusiveTax = args.Checked;
		UpdateSelectedItemFinancialDetails();
	}

	private void UpdateSelectedItemFinancialDetails()
	{
		if (_selectedProduct is null)
			return;

		if (_selectedCart.Quantity <= 0)
			_selectedCart.Quantity = 1;

		_selectedCart.ItemId = _selectedProduct.ProductId;
		_selectedCart.ItemName = _selectedProduct.Name;
		_selectedCart.BaseTotal = _selectedCart.Rate * _selectedCart.Quantity;
		_selectedCart.DiscountAmount = _selectedCart.BaseTotal * (_selectedCart.DiscountPercent / 100);
		_selectedCart.AfterDiscount = _selectedCart.BaseTotal - _selectedCart.DiscountAmount;

		if (_selectedCart.InclusiveTax)
		{
			_selectedCart.CGSTAmount = _selectedCart.AfterDiscount * (_selectedCart.CGSTPercent / (100 + _selectedCart.CGSTPercent));
			_selectedCart.SGSTAmount = _selectedCart.AfterDiscount * (_selectedCart.SGSTPercent / (100 + _selectedCart.SGSTPercent));
			_selectedCart.IGSTAmount = _selectedCart.AfterDiscount * (_selectedCart.IGSTPercent / (100 + _selectedCart.IGSTPercent));
			_selectedCart.TotalTaxAmount = _selectedCart.CGSTAmount + _selectedCart.SGSTAmount + _selectedCart.IGSTAmount;
			_selectedCart.Total = _selectedCart.AfterDiscount;
		}
		else
		{
			_selectedCart.CGSTAmount = _selectedCart.AfterDiscount * (_selectedCart.CGSTPercent / 100);
			_selectedCart.SGSTAmount = _selectedCart.AfterDiscount * (_selectedCart.SGSTPercent / 100);
			_selectedCart.IGSTAmount = _selectedCart.AfterDiscount * (_selectedCart.IGSTPercent / 100);
			_selectedCart.TotalTaxAmount = _selectedCart.CGSTAmount + _selectedCart.SGSTAmount + _selectedCart.IGSTAmount;
			_selectedCart.Total = _selectedCart.AfterDiscount + _selectedCart.TotalTaxAmount;
		}

		StateHasChanged();
	}

	private async Task AddItemToCart()
	{
		if (_selectedProduct is null || _selectedProduct.ProductId <= 0 || _selectedCart.Quantity <= 0 || _selectedCart.Rate < 0 || _selectedCart.DiscountPercent < 0 || _selectedCart.CGSTPercent < 0 || _selectedCart.SGSTPercent < 0 || _selectedCart.IGSTPercent < 0 || _selectedCart.Total < 0)
		{
			await _toastNotification.ShowAsync("Invalid Item Details", "Please ensure all item details are correctly filled before adding to the cart.", ToastType.Error);
			return;
		}

		// Validate that all three taxes cannot be applied together
		int taxCount = 0;
		if (_selectedCart.CGSTPercent > 0) taxCount++;
		if (_selectedCart.SGSTPercent > 0) taxCount++;
		if (_selectedCart.IGSTPercent > 0) taxCount++;

		if (taxCount == 3)
		{
			await _toastNotification.ShowAsync("Invalid Tax Configuration", "All three taxes (CGST, SGST, IGST) cannot be applied together. Use either CGST+SGST or IGST only.", ToastType.Error);
			return;
		}

		UpdateSelectedItemFinancialDetails();

		var existingItem = _cart.FirstOrDefault(s => s.ItemId == _selectedCart.ItemId);
		if (existingItem is not null)
		{
			existingItem.Quantity += _selectedCart.Quantity;
			existingItem.Rate = _selectedCart.Rate;
			existingItem.DiscountPercent = _selectedCart.DiscountPercent;
			existingItem.CGSTPercent = _selectedCart.CGSTPercent;
			existingItem.SGSTPercent = _selectedCart.SGSTPercent;
			existingItem.IGSTPercent = _selectedCart.IGSTPercent;
		}
		else
			_cart.Add(new()
			{
				ItemId = _selectedCart.ItemId,
				ItemName = _selectedCart.ItemName,
				Quantity = _selectedCart.Quantity,
				Rate = _selectedCart.Rate,
				DiscountPercent = _selectedCart.DiscountPercent,
				CGSTPercent = _selectedCart.CGSTPercent,
				SGSTPercent = _selectedCart.SGSTPercent,
				IGSTPercent = _selectedCart.IGSTPercent,
				InclusiveTax = _selectedCart.InclusiveTax,
				Remarks = _selectedCart.Remarks
			});

		_selectedProduct = null;
		_selectedCart = new();

		await _sfItemAutoComplete.FocusAsync();
		await SaveTransactionFile();
	}

	private async Task EditSelectedCartItem()
	{
		if (_sfCartGrid is null || _sfCartGrid.SelectedRecords is null || _sfCartGrid.SelectedRecords.Count == 0)
			return;

		var selectedCartItem = _sfCartGrid.SelectedRecords.First();
		await EditCartItem(selectedCartItem);
	}

	private async Task EditCartItem(SaleItemCartModel cartItem)
	{
		_selectedProduct = _products.FirstOrDefault(s => s.ProductId == cartItem.ItemId);

		if (_selectedProduct is null)
			return;

		_selectedCart = new()
		{
			ItemId = cartItem.ItemId,
			ItemName = cartItem.ItemName,
			Quantity = cartItem.Quantity,
			Rate = cartItem.Rate,
			DiscountPercent = cartItem.DiscountPercent,
			CGSTPercent = cartItem.CGSTPercent,
			SGSTPercent = cartItem.SGSTPercent,
			IGSTPercent = cartItem.IGSTPercent,
			InclusiveTax = cartItem.InclusiveTax,
			Remarks = cartItem.Remarks
		};

		await _sfItemAutoComplete.FocusAsync();
		UpdateSelectedItemFinancialDetails();
		await RemoveItemFromCart(cartItem);
	}

	private async Task RemoveSelectedCartItem()
	{
		if (_sfCartGrid is null || _sfCartGrid.SelectedRecords is null || _sfCartGrid.SelectedRecords.Count == 0)
			return;

		var selectedCartItem = _sfCartGrid.SelectedRecords.First();
		await RemoveItemFromCart(selectedCartItem);
	}

	private async Task RemoveItemFromCart(SaleItemCartModel cartItem)
	{
		_cart.Remove(cartItem);
		await SaveTransactionFile();
	}
	#endregion

	#region Saving
	private async Task UpdateFinancialDetails(bool customRoundOff = false)
	{
		foreach (var item in _cart)
		{
			if (item.Quantity == 0)
				_cart.Remove(item);

			item.BaseTotal = item.Rate * item.Quantity;
			item.DiscountAmount = item.BaseTotal * (item.DiscountPercent / 100);
			item.AfterDiscount = item.BaseTotal - item.DiscountAmount;

			if (item.InclusiveTax)
			{
				item.CGSTAmount = item.AfterDiscount * (item.CGSTPercent / (100 + item.CGSTPercent));
				item.SGSTAmount = item.AfterDiscount * (item.SGSTPercent / (100 + item.SGSTPercent));
				item.IGSTAmount = item.AfterDiscount * (item.IGSTPercent / (100 + item.IGSTPercent));
				item.TotalTaxAmount = item.CGSTAmount + item.SGSTAmount + item.IGSTAmount;
				item.Total = item.AfterDiscount;
			}
			else
			{
				item.CGSTAmount = item.AfterDiscount * (item.CGSTPercent / 100);
				item.SGSTAmount = item.AfterDiscount * (item.SGSTPercent / 100);
				item.IGSTAmount = item.AfterDiscount * (item.IGSTPercent / 100);
				item.TotalTaxAmount = item.CGSTAmount + item.SGSTAmount + item.IGSTAmount;
				item.Total = item.AfterDiscount + item.TotalTaxAmount;
			}

			var perUnitCost = item.Total / item.Quantity;
			var withOtherCharges = perUnitCost * (1 + _sale.OtherChargesPercent / 100);
			item.NetRate = withOtherCharges * (1 - _sale.DiscountPercent / 100);

			item.Remarks = item.Remarks?.Trim();
			if (string.IsNullOrWhiteSpace(item.Remarks))
				item.Remarks = null;
		}

		_sale.TotalItems = _cart.Count;
		_sale.TotalQuantity = _cart.Sum(x => x.Quantity);
		_sale.BaseTotal = _cart.Sum(x => x.BaseTotal);
		_sale.ItemDiscountAmount = _cart.Sum(x => x.DiscountAmount);
		_sale.TotalAfterItemDiscount = _cart.Sum(x => x.AfterDiscount);
		_sale.TotalInclusiveTaxAmount = _cart.Where(x => x.InclusiveTax).Sum(x => x.TotalTaxAmount);
		_sale.TotalExtraTaxAmount = _cart.Where(x => !x.InclusiveTax).Sum(x => x.TotalTaxAmount);
		_sale.TotalAfterTax = _cart.Sum(x => x.Total);

		_sale.OtherChargesAmount = _sale.TotalAfterTax * _sale.OtherChargesPercent / 100;
		var totalAfterOtherCharges = _sale.TotalAfterTax + _sale.OtherChargesAmount;

		_sale.DiscountAmount = totalAfterOtherCharges * _sale.DiscountPercent / 100;
		var totalAfterDiscount = totalAfterOtherCharges - _sale.DiscountAmount;

		if (!customRoundOff)
			_sale.RoundOffAmount = Math.Round(totalAfterDiscount) - totalAfterDiscount;

		_sale.TotalAmount = totalAfterDiscount + _sale.RoundOffAmount;

		var mainCompanyId = await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId);
		_sale.CompanyId = _user.LocationId == 1 ? _selectedCompany.Id : _companies.FirstOrDefault(s => s.Id.ToString() == mainCompanyId.Value).Id;
		_sale.LocationId = _user.LocationId == 1 ? _selectedLocation.Id : _user.LocationId;
		_sale.PartyId = _sale.LocationId == 1 ? _selectedParty?.Id : null;
		_sale.CustomerId = _selectedCustomer?.Id;
		_sale.CreatedBy = _user.Id;
		_sale.TransactionDateTime = _user.LocationId == 1 ? _sale.TransactionDateTime : await CommonData.LoadCurrentDateTime();

		#region Financial Year
		_selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_sale.TransactionDateTime);
		if (_selectedFinancialYear is not null && !_selectedFinancialYear.Locked)
			_sale.FinancialYearId = _selectedFinancialYear.Id;
		else
		{
			await _toastNotification.ShowAsync("Invalid Transaction Date", "The selected transaction date does not fall within an active financial year.", ToastType.Error);
			_sale.TransactionDateTime = await CommonData.LoadCurrentDateTime();
			_selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_sale.TransactionDateTime);
			_sale.FinancialYearId = _selectedFinancialYear.Id;
		}
		#endregion

		if (Id is null)
			_sale.TransactionNo = await GenerateCodes.GenerateSaleTransactionNo(_sale);
	}

	private async Task SaveTransactionFile(bool customRoundOff = false)
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			_isProcessing = true;

			await UpdateFinancialDetails(customRoundOff);

			await DataStorageService.LocalSaveAsync(StorageFileNames.SaleDataFileName, System.Text.Json.JsonSerializer.Serialize(_sale));
			await DataStorageService.LocalSaveAsync(StorageFileNames.SaleCartDataFileName, System.Text.Json.JsonSerializer.Serialize(_cart));
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Saving Transaction Data", ex.Message, ToastType.Error);
		}
		finally
		{
			if (_sfCartGrid is not null)
				await _sfCartGrid?.Refresh();

			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task<bool> ValidateForm()
	{
		if (_user.LocationId > 1 || _sale.LocationId > 1)
		{
			_sale.PartyId = null;
			_selectedParty = null;

			_orders.Clear();
			_selectedOrder = null;
			_sale.OrderId = null;
		}

		if (_user.LocationId > 1)
		{
			_sale.LocationId = _user.LocationId;
			_selectedLocation = _locations.FirstOrDefault(s => s.Id == _user.LocationId);

			var mainCompanyId = await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId);
			_sale.CompanyId = _companies.FirstOrDefault(s => s.Id.ToString() == mainCompanyId.Value).Id;
			_selectedCompany = _companies.FirstOrDefault(s => s.Id.ToString() == mainCompanyId.Value);

			_sale.TransactionDateTime = await CommonData.LoadCurrentDateTime();
		}

		if (_selectedCompany is null || _sale.CompanyId <= 0)
		{
			await _toastNotification.ShowAsync("Company Not Selected", "Please select a company for the transaction.", ToastType.Error);
			return false;
		}

		if (string.IsNullOrWhiteSpace(_sale.TransactionNo))
		{
			await _toastNotification.ShowAsync("Transaction Number Missing", "Please enter a transaction number for the transaction.", ToastType.Error);
			return false;
		}

		if (_sale.TransactionDateTime == default)
		{
			await _toastNotification.ShowAsync("Transaction Date Missing", "Please select a valid transaction date for the transaction.", ToastType.Error);
			return false;
		}

		if (_selectedFinancialYear is null || _sale.FinancialYearId <= 0)
		{
			await _toastNotification.ShowAsync("Financial Year Not Found", "The transaction date does not fall within any financial year. Please check the date and try again.", ToastType.Error);
			return false;
		}

		if (_selectedFinancialYear.Locked)
		{
			await _toastNotification.ShowAsync("Financial Year Locked", "The financial year for the selected transaction date is locked. Please select a different date.", ToastType.Error);
			return false;
		}

		if (_selectedFinancialYear.Status == false)
		{
			await _toastNotification.ShowAsync("Financial Year Inactive", "The financial year for the selected transaction date is inactive. Please select a different date.", ToastType.Error);
			return false;
		}

		if (_sale.TotalItems <= 0)
		{
			await _toastNotification.ShowAsync("Cart is Empty", "Please add at least one item to the cart before saving the transaction.", ToastType.Error);
			return false;
		}

		if (_sale.TotalQuantity <= 0)
		{
			await _toastNotification.ShowAsync("Invalid Total Quantity", "The total quantity of items in the cart must be greater than zero.", ToastType.Error);
			return false;
		}

		if (_sale.TotalAmount < 0)
		{
			await _toastNotification.ShowAsync("Invalid Total Amount", "The total amount of the transaction must be greater than zero.", ToastType.Error);
			return false;
		}

		if (_cart.Any(item => item.Quantity <= 0))
		{
			await _toastNotification.ShowAsync("Invalid Item Quantity", "One or more items in the cart have a quantity less than or equal to zero. Please correct the quantities before saving.", ToastType.Error);
			return false;
		}

		if (_sale.Id > 0)
		{
			var existingSale = await CommonData.LoadTableDataById<SaleModel>(TableNames.Sale, _sale.Id);
			var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, existingSale.FinancialYearId);
			if (financialYear is null || financialYear.Locked || financialYear.Status == false)
			{
				await _toastNotification.ShowAsync("Financial Year Locked or Inactive", "The financial year for the selected transaction date is either locked or inactive. Please select a different date.", ToastType.Error);
				return false;
			}

			if (!_user.Admin || _user.LocationId > 1)
			{
				await _toastNotification.ShowAsync("Insufficient Permissions", "You do not have the necessary permissions to modify this transaction.", ToastType.Error);
				await DeleteLocalFiles();
				NavigationManager.NavigateTo(PageRouteNames.Sale, true);
				return false;
			}
		}

		_sale.Remarks = _sale.Remarks?.Trim();
		if (string.IsNullOrWhiteSpace(_sale.Remarks))
			_sale.Remarks = null;

		if (string.IsNullOrWhiteSpace(_selectedCustomer.Name) && !string.IsNullOrWhiteSpace(_selectedCustomer.Number))
		{
			await _toastNotification.ShowAsync("Customer Name Missing", "Please enter a name for the new customer or clear the customer field.", ToastType.Error);
			return false;
		}

		if (_selectedCustomer.Id > 0)
		{
			_selectedCustomer = await CommonData.LoadTableDataById<CustomerModel>(TableNames.Customer, _selectedCustomer.Id);
			_sale.CustomerId = _selectedCustomer.Id;
		}
		else if (!string.IsNullOrWhiteSpace(_selectedCustomer.Number) && _selectedCustomer.Id == 0)
		{
			_selectedCustomer.Id = await CustomerData.InsertCustomer(_selectedCustomer);
			_sale.CustomerId = _selectedCustomer.Id;
		}
		else
		{
			_selectedCustomer = new();
			_sale.CustomerId = null;
		}

		if (_sale.Cash < 0 || _sale.Card < 0 || _sale.Credit < 0 || _sale.UPI < 0)
		{
			await _toastNotification.ShowAsync("Invalid Payment Amounts", "Payment amounts (Cash, Card, Credit, UPI) cannot be negative. Please correct the amounts before saving.", ToastType.Error);
			return false;
		}

		if (_sale.Cash + _sale.Card + _sale.Credit + _sale.UPI != _sale.TotalAmount)
		{
			await _toastNotification.ShowAsync("Payment Amount Mismatch", "The sum of payment amounts (Cash, Card, Credit, UPI) must equal the total amount of the transaction. Please correct the amounts before saving.", ToastType.Error);
			return false;
		}

		if (_sale.Credit > 0 && (_selectedParty is null || _sale.PartyId is null || _sale.PartyId <= 0))
		{
			await _toastNotification.ShowAsync("Party Not Selected for Credit Payment", "Please select a party ledger for credit payment method.", ToastType.Error);
			return false;
		}

		if (_selectedOrder is null)
			_sale.OrderId = null;

		if (_selectedOrder is not null && _selectedOrder.LocationId != _selectedParty.LocationId)
		{
			await _toastNotification.ShowAsync("Order Location Mismatch", "The selected order does not belong to the selected party's location. Please select a valid order.", ToastType.Error);
			return false;
		}
		else if (_selectedOrder is not null)
			_sale.OrderId = _selectedOrder.Id;

		return true;
	}

	private async Task SaveTransaction()
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			_isProcessing = true;

			await SaveTransactionFile(true);

			if (!await ValidateForm())
			{
				_isProcessing = false;
				return;
			}

			await _toastNotification.ShowAsync("Processing Transaction", "Please wait while the transaction is being saved...", ToastType.Info);

			_sale.Status = true;
			var currentDateTime = await CommonData.LoadCurrentDateTime();
			_sale.TransactionDateTime = DateOnly.FromDateTime(_sale.TransactionDateTime).ToDateTime(new TimeOnly(currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second));
			_sale.LastModifiedAt = currentDateTime;
			_sale.CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
			_sale.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
			_sale.CreatedBy = _user.Id;
			_sale.LastModifiedBy = _user.Id;

			_sale.Id = await SaleData.SaveSaleTransaction(_sale, _cart);
			var (pdfStream, fileName) = await SaleData.GenerateAndDownloadInvoice(_sale.Id);
			await SaveAndViewService.SaveAndView(fileName, pdfStream);
			await DeleteLocalFiles();
			NavigationManager.NavigateTo(PageRouteNames.Sale, true);

			await _toastNotification.ShowAsync("Save Transaction", "Transaction saved successfully! Invoice has been generated.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Saving Transaction", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
		}
	}

	private async Task DeleteLocalFiles()
	{
		await DataStorageService.LocalRemove(StorageFileNames.SaleDataFileName);
		await DataStorageService.LocalRemove(StorageFileNames.SaleCartDataFileName);
	}
	#endregion

	#region Utilities
	private async Task DownloadPdfInvoice()
	{
		if (!Id.HasValue || Id.Value <= 0)
		{
			await _toastNotification.ShowAsync("No Transaction Selected", "Please save the transaction first before downloading the invoice.", ToastType.Error);
			return;
		}

		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating PDF invoice...", ToastType.Info);
			var (pdfStream, fileName) = await SaleData.GenerateAndDownloadInvoice(Id.Value);
			await SaveAndViewService.SaveAndView(fileName, pdfStream);
			await _toastNotification.ShowAsync("Invoice Downloaded", "The PDF invoice has been downloaded successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Downloading Invoice", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
		}
	}

	private async Task DownloadExcelInvoice()
	{
		if (!Id.HasValue || Id.Value <= 0)
		{
			await _toastNotification.ShowAsync("No Transaction Selected", "Please save the transaction first before downloading the invoice.", ToastType.Error);
			return;
		}

		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating Excel invoice...", ToastType.Info);
			var (excelStream, fileName) = await SaleData.GenerateAndDownloadExcelInvoice(Id.Value);
			await SaveAndViewService.SaveAndView(fileName, excelStream);
			await _toastNotification.ShowAsync("Invoice Downloaded", "The Excel invoice has been downloaded successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Downloading Invoice", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
		}
	}

	private async Task ResetPage()
	{
		await DeleteLocalFiles();
		NavigationManager.NavigateTo(PageRouteNames.Sale, true);
	}

	private async Task NavigateToTransactionHistoryPage()
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportSale, "_blank");
		else
			NavigationManager.NavigateTo(PageRouteNames.ReportSale);
	}

	private async Task NavigateToItemReport()
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportSaleItem, "_blank");
		else
			NavigationManager.NavigateTo(PageRouteNames.ReportSaleItem);
	}

	private async Task NavigateToSelectedOrderPage()
	{
		if (_selectedOrder is null)
		{
			await _toastNotification.ShowAsync("No Order Selected", "Please select an order to view its details.", ToastType.Error);
			return;
		}

		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", $"{PageRouteNames.Order}/{_selectedOrder.Id}", "_blank");
		else
			NavigationManager.NavigateTo($"{PageRouteNames.Order}/{_selectedOrder.Id}");
	}

	private async Task DownloadSelectedOrderPdf()
	{
		if (_selectedOrder is null)
		{
			await _toastNotification.ShowAsync("No Order Selected", "Please select an order to download its invoice.", ToastType.Error);
			return;
		}

		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating PDF invoice...", ToastType.Info);
			var (pdfStream, fileName) = await OrderData.GenerateAndDownloadInvoice(_selectedOrder.Id);
			await SaveAndViewService.SaveAndView(fileName, pdfStream);
			await _toastNotification.ShowAsync("Invoice Downloaded", "The PDF invoice has been downloaded successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Downloading Invoice", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
		}
	}

	private async Task DownloadSelectedOrderExcel()
	{
		if (_selectedOrder is null)
		{
			await _toastNotification.ShowAsync("No Order Selected", "Please select an order to download its invoice.", ToastType.Error);
			return;
		}

		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating Excel invoice...", ToastType.Info);
			var (excelStream, fileName) = await OrderData.GenerateAndDownloadExcelInvoice(_selectedOrder.Id);
			await SaveAndViewService.SaveAndView(fileName, excelStream);
			await _toastNotification.ShowAsync("Invoice Downloaded", "The Excel invoice has been downloaded successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Downloading Invoice", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
		}
	}

	private async Task NavigateToDashboard() =>
		NavigationManager.NavigateTo(PageRouteNames.Dashboard);

	private async Task NavigateBack() =>
		NavigationManager.NavigateTo(PageRouteNames.SalesDashboard);

	public async ValueTask DisposeAsync()
	{
		if (_hotKeysContext is not null)
			await _hotKeysContext.DisposeAsync();
	}
	#endregion
}