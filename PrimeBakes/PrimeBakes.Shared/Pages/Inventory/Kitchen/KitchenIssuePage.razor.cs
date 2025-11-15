using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Kitchen;
using PrimeBakesLibrary.Data.Inventory.Purchase;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Inventory.Kitchen;

using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Inputs;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Inventory.Kitchen;

public partial class KitchenIssuePage
{
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

	private List<CompanyModel> _companies = [];
	private List<KitchenModel> _kitchens = [];
	private List<RawMaterialModel> _rawMaterials = [];
	private List<KitchenIssueItemCartModel> _cart = [];

	private SfAutoComplete<RawMaterialModel?, RawMaterialModel> _sfItemAutoComplete;
	private SfGrid<KitchenIssueItemCartModel> _sfCartGrid;

	private string _errorTitle = string.Empty;
	private string _errorMessage = string.Empty;

	private string _successTitle = string.Empty;
	private string _successMessage = string.Empty;

	private SfToast _sfSuccessToast;
	private SfToast _sfErrorToast;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Inventory, true);
		_user = authResult.User;
		await LoadData();
		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		await LoadCompanies();
		await LoadKitchens();
		await LoadExistingKitchenIssue();
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
			await ShowToast("An Error Occurred While Loading Companies", ex.Message, "error");
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
			await ShowToast("An Error Occurred While Loading Ledgers", ex.Message, "error");
		}
	}

	private async Task LoadExistingKitchenIssue()
	{
		try
		{
			if (Id.HasValue)
			{
				_kitchenIssue = await CommonData.LoadTableDataById<KitchenIssueModel>(TableNames.KitchenIssue, Id.Value);
				if (_kitchenIssue is null)
				{
					await ShowToast("Kitchen Issue Not Found", "The requested kitchen issue could not be found.", "error");
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
			await ShowToast("An Error Occurred While Loading Kitchen Issue Data", ex.Message, "error");
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
		}
		catch (Exception ex)
		{
			await ShowToast("An Error Occurred While Loading Items", ex.Message, "error");
		}
	}

	private async Task LoadExistingCart()
	{
		try
		{
			_cart.Clear();

			if (_kitchenIssue.Id > 0)
			{
				var existingCart = await KitchenIssueData.LoadKitchenIssueDetailByKitchenIssue(_kitchenIssue.Id);

				foreach (var item in existingCart)
				{
					if (_rawMaterials.FirstOrDefault(s => s.Id == item.RawMaterialId) is null)
					{
						await ShowToast("Item Not Found", $"The item with ID {item.RawMaterialId} in the existing kitchen issue cart was not found in the available items list. It may have been deleted or is inaccessible.", "error");
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
			await ShowToast("An Error Occurred While Loading Existing Cart", ex.Message, "error");
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
			await ShowToast("Invalid Item Details", "Please ensure all item details are correctly filled before adding to the cart.", "error");
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
		}

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
			await ShowToast("Invalid Transaction Date", "The selected transaction date does not fall within an active financial year.", "error");
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
			await ShowToast("An Error Occurred While Saving Transaction Data", ex.Message, "error");
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
		if (_cart.Count == 0)
		{
			await ShowToast("Cart is Empty", "Please add at least one item to the cart before saving the transaction.", "error");
			return false;
		}

		if (_selectedCompany is null || _kitchenIssue.CompanyId <= 0)
		{
			await ShowToast("Company Not Selected", "Please select a company for the kitchen issue transaction.", "error");
			return false;
		}

		if (_selectedKitchen is null || _kitchenIssue.KitchenId <= 0)
		{
			await ShowToast("Kitchen Not Selected", "Please select a kitchen for the kitchen issue transaction.", "error");
			return false;
		}

		if (string.IsNullOrWhiteSpace(_kitchenIssue.TransactionNo))
		{
			await ShowToast("Transaction Number Missing", "Please enter a transaction number for the kitchen issue.", "error");
			return false;
		}

		if (_kitchenIssue.TransactionDateTime == default)
		{
			await ShowToast("Transaction Date Missing", "Please select a valid transaction date for the kitchen issue.", "error");
			return false;
		}

		if (_selectedFinancialYear is null || _kitchenIssue.FinancialYearId <= 0)
		{
			await ShowToast("Financial Year Not Found", "The transaction date does not fall within any financial year. Please check the date and try again.", "error");
			return false;
		}

		if (_selectedFinancialYear.Locked)
		{
			await ShowToast("Financial Year Locked", "The financial year for the selected transaction date is locked. Please select a different date.", "error");
			return false;
		}

		if (_selectedFinancialYear.Status == false)
		{
			await ShowToast("Financial Year Inactive", "The financial year for the selected transaction date is inactive. Please select a different date.", "error");
			return false;
		}

		if (_kitchenIssue.TotalAmount < 0)
		{
			await ShowToast("Invalid Total Amount", "The total amount of the kitchen issue transaction must be greater than zero.", "error");
			return false;
		}

		if (_cart.Any(item => item.Quantity <= 0))
		{
			await ShowToast("Invalid Item Quantity", "One or more items in the cart have a quantity less than or equal to zero. Please correct the quantities before saving.", "error");
			return false;
		}

		if (_kitchenIssue.Id > 0)
		{
			var existingKitchenIssue = await CommonData.LoadTableDataById<KitchenIssueModel>(TableNames.KitchenIssue, _kitchenIssue.Id);
			var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, _kitchenIssue.FinancialYearId);
			if (financialYear is null || financialYear.Locked || financialYear.Status == false)
			{
				await ShowToast("Financial Year Locked or Inactive", "The financial year for the selected transaction date is either locked or inactive. Please select a different date.", "error");
				return false;
			}

			if (!_user.Admin)
			{
				await ShowToast("Insufficient Permissions", "You do not have the necessary permissions to modify this transaction.", "error");
				return false;
			}
		}

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

			await ShowToast("Save Transaction", "Transaction saved successfully! Invoice has been generated.", "success");
		}
		catch (Exception ex)
		{
			await ShowToast("An Error Occurred While Saving Transaction", ex.Message, "error");
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
	private async Task ResetPage(Microsoft.AspNetCore.Components.Web.MouseEventArgs args)
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

	private async Task ShowToast(string title, string message, string type)
	{
		VibrationService.VibrateWithTime(200);

		if (type == "error")
		{
			_errorTitle = title;
			_errorMessage = message;
			await _sfErrorToast.ShowAsync(new()
			{
				Title = _errorTitle,
				Content = _errorMessage
			});
		}

		else if (type == "success")
		{
			_successTitle = title;
			_successMessage = message;
			await _sfSuccessToast.ShowAsync(new()
			{
				Title = _successTitle,
				Content = _successMessage
			});
		}
	}
	#endregion
}