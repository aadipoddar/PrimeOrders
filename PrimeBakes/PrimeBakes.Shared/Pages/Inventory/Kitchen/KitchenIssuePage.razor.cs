using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakesLibrary.Data;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Kitchen;
using PrimeBakesLibrary.Data.Inventory.Purchase;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Inventory.Stock;

using PrimeBakes.Shared.Components;

using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Inputs;

namespace PrimeBakes.Shared.Pages.Inventory.Kitchen;

public partial class KitchenIssuePage : IAsyncDisposable
{
	private HotKeysContext _hotKeysContext;

	[Parameter] public int? Id { get; set; }

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;

	private decimal _itemAfterTaxTotal = 0;

	private CompanyModel _selectedCompany = new();
	private KitchenModel _selectedKitchen = new();
	private FinancialYearModel _selectedFinancialYear = new();
	private RawMaterialModel? _selectedRawMaterial = new();
	private KitchenIssueItemCartModel _selectedCart = new();
	private KitchenIssueModel _kitchenIssue = new();

	private List<RawMaterialStockSummaryModel> _stockSummary = [];
	private List<CompanyModel> _companies = [];
	private List<KitchenModel> _kitchens = [];
	private List<RawMaterialModel> _rawMaterials = [];
	private List<KitchenIssueItemCartModel> _cart = [];

	private SfAutoComplete<RawMaterialModel?, RawMaterialModel> _sfItemAutoComplete;
	private SfGrid<KitchenIssueItemCartModel> _sfCartGrid;

	private ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Inventory, true);
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
			.Add(ModCode.Ctrl, Code.P, DownloadInvoice, "Download invoice", Exclude.None)
			.Add(ModCode.Ctrl, Code.H, NavigateToTransactionHistoryPage, "Open transaction history", Exclude.None)
			.Add(ModCode.Ctrl, Code.I, NavigateToItemReport, "Open item report", Exclude.None)
			.Add(ModCode.Ctrl, Code.N, ResetPage, "Reset the page", Exclude.None)
			.Add(ModCode.Ctrl, Code.D, NavigateToDashboard, "Go to dashboard", Exclude.None)
			.Add(ModCode.Ctrl, Code.B, NavigateBack, "Back", Exclude.None)
			.Add(Code.Delete, RemoveSelectedCartItem, "Delete selected cart item", Exclude.None)
			.Add(Code.Insert, EditSelectedCartItem, "Edit selected cart item", Exclude.None);

		await LoadCompanies();
		await LoadKitchens();
		await LoadExistingTransaction();
		await LoadItems();
		await LoadExistingCart();
		await SaveTransactionFile();
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

	private async Task LoadKitchens()
	{
		try
		{
			_kitchens = await CommonData.LoadTableDataByStatus<KitchenModel>(TableNames.Kitchen);
			_kitchens = [.. _kitchens.OrderBy(s => s.Name)];
			_kitchens.Add(new()
			{
				Id = 0,
				Name = "Create New Kitchen ..."
			});

			_selectedKitchen = _kitchens.FirstOrDefault();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Kitchens", ex.Message, ToastType.Error);
		}
	}

	private async Task LoadExistingTransaction()
	{
		try
		{
			if (Id.HasValue)
			{
				_kitchenIssue = await CommonData.LoadTableDataById<KitchenIssueModel>(TableNames.KitchenIssue, Id.Value);
				if (_kitchenIssue is null)
				{
					await _toastNotification.ShowAsync("Transaction Not Found", "The requested transaction could not be found.", ToastType.Error);
					NavigationManager.NavigateTo(PageRouteNames.KitchenIssue, true);
				}
			}

			else if (await DataStorageService.LocalExists(StorageFileNames.KitchenIssueDataFileName))
				_kitchenIssue = System.Text.Json.JsonSerializer.Deserialize<KitchenIssueModel>(await DataStorageService.LocalGetAsync(StorageFileNames.KitchenIssueDataFileName));

			else
			{
				_kitchenIssue = new()
				{
					Id = 0,
					TransactionNo = string.Empty,
					CompanyId = _selectedCompany.Id,
					KitchenId = _selectedKitchen.Id,
					TransactionDateTime = await CommonData.LoadCurrentDateTime(),
					FinancialYearId = (await FinancialYearData.LoadFinancialYearByDateTime(await CommonData.LoadCurrentDateTime())).Id,
					CreatedBy = _user.Id,
					TotalItems = 0,
					TotalQuantity = 0,
					TotalAmount = 0,
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

			if (_kitchenIssue.CompanyId > 0)
				_selectedCompany = _companies.FirstOrDefault(s => s.Id == _kitchenIssue.CompanyId);
			else
			{
				_selectedCompany = _companies.FirstOrDefault();
				_kitchenIssue.CompanyId = _selectedCompany.Id;
			}

			if (_kitchenIssue.KitchenId > 0)
				_selectedKitchen = _kitchens.FirstOrDefault(s => s.Id == _kitchenIssue.KitchenId);
			else
			{
				_selectedKitchen = _kitchens.FirstOrDefault();
				_kitchenIssue.KitchenId = _selectedKitchen.Id;
			}

			_selectedFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, _kitchenIssue.FinancialYearId);
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
			_rawMaterials = await PurchaseData.LoadRawMaterialByPartyPurchaseDateTime(0, _kitchenIssue.TransactionDateTime);
			_rawMaterials = [.. _rawMaterials.OrderBy(s => s.Name)];
			_rawMaterials.Add(new()
			{
				Id = 0,
				Name = "Create New Item ..."
			});

			_stockSummary = await RawMaterialStockData.LoadRawMaterialStockSummaryByDate(_kitchenIssue.TransactionDateTime, _kitchenIssue.TransactionDateTime);
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

			if (_kitchenIssue.Id > 0)
			{
				var existingCart = await CommonData.LoadTableDataByMasterId<KitchenIssueDetailModel>(TableNames.KitchenIssueDetail, _kitchenIssue.Id);

				foreach (var item in existingCart)
				{
					if (_rawMaterials.FirstOrDefault(s => s.Id == item.RawMaterialId) is null)
					{
						var rawMaterial = await CommonData.LoadTableDataById<RawMaterialModel>(TableNames.RawMaterial, item.RawMaterialId);

						await _toastNotification.ShowAsync("Item Not Found", $"The item {rawMaterial?.Name} (ID: {item.RawMaterialId}) in the existing transaction cart was not found in the available items list. It may have been deleted or is inaccessible.", ToastType.Error);
						continue;
					}

					_cart.Add(new()
					{
						ItemId = item.RawMaterialId,
						ItemName = _rawMaterials.FirstOrDefault(s => s.Id == item.RawMaterialId)?.Name ?? "",
						Quantity = item.Quantity,
						UnitOfMeasurement = item.UnitOfMeasurement,
						Rate = item.Rate,
						Total = item.Total,
						Remarks = item.Remarks
					});
				}
			}

			else if (await DataStorageService.LocalExists(StorageFileNames.KitchenIssueCartDataFileName))
				_cart = System.Text.Json.JsonSerializer.Deserialize<List<KitchenIssueItemCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.KitchenIssueCartDataFileName));
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
	private async Task OnCompanyChanged(ChangeEventArgs<CompanyModel, CompanyModel> args)
	{
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
		_kitchenIssue.CompanyId = _selectedCompany.Id;

		await SaveTransactionFile();
	}

	private async Task OnKitchenChanged(ChangeEventArgs<KitchenModel, KitchenModel> args)
	{
		if (args.Value is null)
			return;

		if (args.Value.Id == 0)
		{
			if (FormFactor.GetFormFactor() == "Web")
				await JSRuntime.InvokeVoidAsync("open", PageRouteNames.AdminKitchen, "_blank");
			else
				NavigationManager.NavigateTo(PageRouteNames.AdminKitchen);

			return;
		}

		_selectedKitchen = args.Value;
		_kitchenIssue.KitchenId = _selectedKitchen.Id;

		await LoadItems();
		await SaveTransactionFile();
	}

	private async Task OnTransactionDateChanged(Syncfusion.Blazor.Calendars.ChangedEventArgs<DateTime> args)
	{
		_kitchenIssue.TransactionDateTime = args.Value;
		await LoadItems();
		await SaveTransactionFile();
	}
	#endregion

	#region Cart
	private async Task OnItemChanged(ChangeEventArgs<RawMaterialModel?, RawMaterialModel> args)
	{
		if (args.Value is null)
			return;

		if (args.Value.Id == 0)
		{
			if (FormFactor.GetFormFactor() == "Web")
				await JSRuntime.InvokeVoidAsync("open", PageRouteNames.AdminRawMaterial, "_blank");
			else
				NavigationManager.NavigateTo(PageRouteNames.AdminRawMaterial);

			return;
		}

		_selectedRawMaterial = args.Value;

		if (_selectedRawMaterial is null)
			_selectedCart = new()
			{
				ItemId = 0,
				ItemName = "",
				Quantity = 1,
				UnitOfMeasurement = "",
				Rate = 0
			};

		else
		{
			_selectedCart.ItemId = _selectedRawMaterial.Id;
			_selectedCart.ItemName = _selectedRawMaterial.Name;
			_selectedCart.Quantity = 1;
			_selectedCart.UnitOfMeasurement = _selectedRawMaterial.UnitOfMeasurement;
			_selectedCart.Rate = _selectedRawMaterial.Rate;
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

	private void UpdateSelectedItemFinancialDetails()
	{
		if (_selectedRawMaterial is null)
			return;

		if (_selectedCart.Quantity <= 0)
			_selectedCart.Quantity = 1;

		if (string.IsNullOrWhiteSpace(_selectedCart.UnitOfMeasurement))
			_selectedCart.UnitOfMeasurement = _selectedRawMaterial.UnitOfMeasurement;

		_selectedCart.ItemId = _selectedRawMaterial.Id;
		_selectedCart.ItemName = _selectedRawMaterial.Name;
		_selectedCart.Total = _selectedRawMaterial.Rate * _selectedCart.Quantity;

		StateHasChanged();
	}

	private async Task AddItemToCart()
	{
		if (_selectedRawMaterial is null || _selectedRawMaterial.Id <= 0 || _selectedCart.Quantity <= 0 || _selectedCart.Rate < 0 || _selectedCart.Total < 0 || string.IsNullOrEmpty(_selectedCart.UnitOfMeasurement))
		{
			await _toastNotification.ShowAsync("Invalid Item Details", "Please ensure all item details are correctly filled before adding to the cart.", ToastType.Error);
			return;
		}

		UpdateSelectedItemFinancialDetails();

		var existingItem = _cart.FirstOrDefault(s => s.ItemId == _selectedCart.ItemId);
		if (existingItem is not null)
		{
			existingItem.Quantity += _selectedCart.Quantity;
			existingItem.Rate = _selectedCart.Rate;
		}
		else
			_cart.Add(new()
			{
				ItemId = _selectedCart.ItemId,
				ItemName = _selectedCart.ItemName,
				Quantity = _selectedCart.Quantity,
				UnitOfMeasurement = _selectedCart.UnitOfMeasurement,
				Rate = _selectedCart.Rate,
				Remarks = _selectedCart.Remarks
			});

		_selectedRawMaterial = null;
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

	private async Task EditCartItem(KitchenIssueItemCartModel cartItem)
	{
		_selectedRawMaterial = _rawMaterials.FirstOrDefault(s => s.Id == cartItem.ItemId);

		if (_selectedRawMaterial is null)
			return;

		_selectedCart = new()
		{
			ItemId = cartItem.ItemId,
			ItemName = cartItem.ItemName,
			Quantity = cartItem.Quantity,
			UnitOfMeasurement = cartItem.UnitOfMeasurement,
			Rate = cartItem.Rate,
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

	private async Task RemoveItemFromCart(KitchenIssueItemCartModel cartItem)
	{
		_cart.Remove(cartItem);
		await SaveTransactionFile();
	}
	#endregion

	#region Saving
	private async Task UpdateFinancialDetails()
	{
		foreach (var item in _cart)
		{
			if (item.Quantity == 0)
				_cart.Remove(item);

			item.Total = item.Rate * item.Quantity;

			item.Remarks = item.Remarks?.Trim();
			if (string.IsNullOrWhiteSpace(item.Remarks))
				item.Remarks = null;
		}

		_kitchenIssue.TotalItems = _cart.Count;
		_kitchenIssue.TotalQuantity = _cart.Sum(x => x.Quantity);
		_kitchenIssue.TotalAmount = _cart.Sum(x => x.Total);
		_itemAfterTaxTotal = _cart.Sum(x => x.Total);

		_kitchenIssue.CompanyId = _selectedCompany.Id;
		_kitchenIssue.KitchenId = _selectedKitchen.Id;
		_kitchenIssue.CreatedBy = _user.Id;

		#region Financial Year
		_selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_kitchenIssue.TransactionDateTime);
		if (_selectedFinancialYear is not null && !_selectedFinancialYear.Locked)
			_kitchenIssue.FinancialYearId = _selectedFinancialYear.Id;
		else
		{
			await _toastNotification.ShowAsync("Invalid Transaction Date", "The selected transaction date does not fall within an active financial year.", ToastType.Error);
			_kitchenIssue.TransactionDateTime = await CommonData.LoadCurrentDateTime();
			_selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_kitchenIssue.TransactionDateTime);
			_kitchenIssue.FinancialYearId = _selectedFinancialYear.Id;
		}
		#endregion

		if (Id is null)
			_kitchenIssue.TransactionNo = await GenerateCodes.GenerateKitchenIssueTransactionNo(_kitchenIssue);
	}

	private async Task SaveTransactionFile()
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			_isProcessing = true;

			await UpdateFinancialDetails();

			await DataStorageService.LocalSaveAsync(StorageFileNames.KitchenIssueDataFileName, System.Text.Json.JsonSerializer.Serialize(_kitchenIssue));
			await DataStorageService.LocalSaveAsync(StorageFileNames.KitchenIssueCartDataFileName, System.Text.Json.JsonSerializer.Serialize(_cart));
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
		if (_selectedCompany is null || _kitchenIssue.CompanyId <= 0)
		{
			await _toastNotification.ShowAsync("Company Not Selected", "Please select a company for the transaction.", ToastType.Warning);
			return false;
		}

		if (_selectedKitchen is null || _kitchenIssue.KitchenId <= 0)
		{
			await _toastNotification.ShowAsync("Kitchen Not Selected", "Please select a kitchen for the transaction.", ToastType.Warning);
			return false;
		}

		if (string.IsNullOrWhiteSpace(_kitchenIssue.TransactionNo))
		{
			await _toastNotification.ShowAsync("Transaction Number Missing", "Please enter a transaction number for the transaction.", ToastType.Warning);
			return false;
		}

		if (_kitchenIssue.TransactionDateTime == default)
		{
			await _toastNotification.ShowAsync("Transaction Date Missing", "Please select a valid transaction date for the transaction.", ToastType.Warning);
			return false;
		}

		if (_selectedFinancialYear is null || _kitchenIssue.FinancialYearId <= 0)
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

		if (_kitchenIssue.TotalItems <= 0)
		{
			await _toastNotification.ShowAsync("No Items in Cart", "The transaction must contain at least one item in the cart.", ToastType.Warning);
			return false;
		}

		if (_kitchenIssue.TotalQuantity <= 0)
		{
			await _toastNotification.ShowAsync("Invalid Total Quantity", "The total quantity of the transaction must be greater than zero.", ToastType.Error);
			return false;
		}

		if (_kitchenIssue.TotalAmount < 0)
		{
			await _toastNotification.ShowAsync("Invalid Total Amount", "The total amount of the transaction must be greater than zero.", ToastType.Error);
			return false;
		}

		if (_cart.Any(item => item.Quantity <= 0))
		{
			await _toastNotification.ShowAsync("Invalid Item Quantity", "One or more items in the cart have a quantity less than or equal to zero. Please correct the quantities before saving.", ToastType.Error);
			return false;
		}

		if (_kitchenIssue.Id > 0)
		{
			var existingKitchenIssue = await CommonData.LoadTableDataById<KitchenIssueModel>(TableNames.KitchenIssue, _kitchenIssue.Id);
			var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, _kitchenIssue.FinancialYearId);
			if (financialYear is null || financialYear.Locked || financialYear.Status == false)
			{
				await _toastNotification.ShowAsync("Financial Year Locked or Inactive", "The financial year for the selected transaction date is either locked or inactive. Please select a different date.", ToastType.Error);
				return false;
			}

			if (!_user.Admin)
			{
				await _toastNotification.ShowAsync("Insufficient Permissions", "You do not have the necessary permissions to modify this transaction.", ToastType.Error);
				return false;
			}
		}

		_kitchenIssue.Remarks = _kitchenIssue.Remarks?.Trim();
		if (string.IsNullOrWhiteSpace(_kitchenIssue.Remarks))
			_kitchenIssue.Remarks = null;

		return true;
	}

	private async Task SaveTransaction()
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			_isProcessing = true;

			await SaveTransactionFile();

			if (!await ValidateForm())
			{
				_isProcessing = false;
				return;
			}

			await _toastNotification.ShowAsync("Processing Transaction", "Please wait while the transaction is being saved...", ToastType.Info);

			_kitchenIssue.Status = true;
			var currentDateTime = await CommonData.LoadCurrentDateTime();
			_kitchenIssue.TransactionDateTime = DateOnly.FromDateTime(_kitchenIssue.TransactionDateTime).ToDateTime(new TimeOnly(currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second));
			_kitchenIssue.LastModifiedAt = currentDateTime;
			_kitchenIssue.CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
			_kitchenIssue.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
			_kitchenIssue.CreatedBy = _user.Id;
			_kitchenIssue.LastModifiedBy = _user.Id;

			_kitchenIssue.Id = await KitchenIssueData.SaveKitchenIssueTransaction(_kitchenIssue, _cart);
			var (pdfStream, fileName) = await KitchenIssueData.GenerateAndDownloadInvoice(_kitchenIssue.Id);
			await SaveAndViewService.SaveAndView(fileName, pdfStream);
			await DeleteLocalFiles();
			NavigationManager.NavigateTo(PageRouteNames.KitchenIssue, true);

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
		await DataStorageService.LocalRemove(StorageFileNames.KitchenIssueDataFileName);
		await DataStorageService.LocalRemove(StorageFileNames.KitchenIssueCartDataFileName);
	}
	#endregion

	#region Utilities
	private async Task ResetPage()
	{
		await DeleteLocalFiles();
		NavigationManager.NavigateTo(PageRouteNames.KitchenIssue, true);
	}

	private async Task NavigateToTransactionHistoryPage()
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportKitchenIssue, "_blank");
		else
			NavigationManager.NavigateTo(PageRouteNames.ReportKitchenIssue);
	}

	private async Task NavigateToItemReport()
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportKitchenIssueItem, "_blank");
		else
			NavigationManager.NavigateTo(PageRouteNames.ReportKitchenIssueItem);
	}

	private async Task DownloadInvoice()
	{
		if (!Id.HasValue || Id.Value <= 0)
		{
			await _toastNotification.ShowAsync("No Transaction Selected", "Please save the transaction first before downloading the invoice.", ToastType.Warning);
			return;
		}

		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating invoice...", ToastType.Info);
			var (pdfStream, fileName) = await KitchenIssueData.GenerateAndDownloadInvoice(Id.Value);
			await SaveAndViewService.SaveAndView(fileName, pdfStream);
			await _toastNotification.ShowAsync("Invoice Downloaded", "The invoice has been downloaded successfully.", ToastType.Success);
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
		NavigationManager.NavigateTo(PageRouteNames.InventoryDashboard);

	public async ValueTask DisposeAsync()
	{
		if (_hotKeysContext is not null)
			await _hotKeysContext.DisposeAsync();
	}
	#endregion
}