using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Kitchen;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Sales.Product;

using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Inputs;
using Syncfusion.Blazor.Notifications;

using Toolbelt.Blazor.HotKeys2;

namespace PrimeBakes.Shared.Pages.Inventory.Kitchen;

public partial class KitchenProductionPage : IAsyncDisposable
{
    [Inject] private HotKeys HotKeys { get; set; }
    private HotKeysContext _hotKeysContext;

    [Parameter] public int? Id { get; set; }

    private UserModel _user;

    private bool _isLoading = true;
    private bool _isProcessing = false;

    private decimal _itemAfterTaxTotal = 0;

    private CompanyModel _selectedCompany = new();
    private KitchenModel _selectedKitchen = new();
    private FinancialYearModel _selectedFinancialYear = new();
    private ProductModel? _selectedProduct = new();
    private KitchenProductionProductCartModel _selectedCart = new();
    private KitchenProductionModel _kitchenProduction = new();

    private List<CompanyModel> _companies = [];
    private List<KitchenModel> _kitchens = [];
    private List<ProductModel> _products = [];
    private List<KitchenProductionProductCartModel> _cart = [];

    private SfAutoComplete<ProductModel?, ProductModel> _sfItemAutoComplete;
    private SfGrid<KitchenProductionProductCartModel> _sfCartGrid;

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

        _user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Inventory, true);
        await LoadData();
        _isLoading = false;
        StateHasChanged();
    }

    private async Task LoadData()
    {
        _hotKeysContext = HotKeys.CreateContext()
            .Add(ModCode.Ctrl, Code.A, AddItemToCart, "Add item to cart", Exclude.None)
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
            await ShowToast("An Error Occurred While Loading Kitchens", ex.Message, "error");
        }
    }

    private async Task LoadExistingTransaction()
    {
        try
        {
            if (Id.HasValue)
            {
                _kitchenProduction = await CommonData.LoadTableDataById<KitchenProductionModel>(TableNames.KitchenProduction, Id.Value);
                if (_kitchenProduction is null)
                {
                    await ShowToast("Transaction Not Found", "The requested transaction could not be found.", "error");
                    NavigationManager.NavigateTo(PageRouteNames.KitchenProduction, true);
                }
            }

            else if (await DataStorageService.LocalExists(StorageFileNames.KitchenProductionDataFileName))
                _kitchenProduction = System.Text.Json.JsonSerializer.Deserialize<KitchenProductionModel>(await DataStorageService.LocalGetAsync(StorageFileNames.KitchenProductionDataFileName));

            else
            {
                _kitchenProduction = new()
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

            if (_kitchenProduction.CompanyId > 0)
                _selectedCompany = _companies.FirstOrDefault(s => s.Id == _kitchenProduction.CompanyId);
            else
            {
                _selectedCompany = _companies.FirstOrDefault();
                _kitchenProduction.CompanyId = _selectedCompany.Id;
            }

            if (_kitchenProduction.KitchenId > 0)
                _selectedKitchen = _kitchens.FirstOrDefault(s => s.Id == _kitchenProduction.KitchenId);
            else
            {
                _selectedKitchen = _kitchens.FirstOrDefault();
                _kitchenProduction.KitchenId = _selectedKitchen.Id;
            }

            _selectedFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, _kitchenProduction.FinancialYearId);
        }
        catch (Exception ex)
        {
            await ShowToast("An Error Occurred While Loading Transaction Data", ex.Message, "error");
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
            _products = await CommonData.LoadTableDataByStatus<ProductModel>(TableNames.Product);
            _products = [.. _products.OrderBy(s => s.Name)];
            _products.Add(new()
            {
                Id = 0,
                Name = "Create New Product ..."
            });
        }
        catch (Exception ex)
        {
            await ShowToast("An Error Occurred While Loading Products", ex.Message, "error");
        }
    }

    private async Task LoadExistingCart()
    {
        try
        {
            _cart.Clear();

            if (_kitchenProduction.Id > 0)
            {
                var existingCart = await CommonData.LoadTableDataByMasterId<KitchenProductionProductCartModel>(TableNames.KitchenProductionDetail, _kitchenProduction.Id);

                foreach (var item in existingCart)
                {
                    if (_products.FirstOrDefault(s => s.Id == item.ProductId) is null)
                    {
                        var product = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, item.ProductId);

                        await ShowToast("Product Not Found", $"The product {product?.Name} (ID: {item.ProductId}) in the existing transaction cart was not found in the available products list. It may have been deleted or is inaccessible.", "error");
                        continue;
                    }

                    _cart.Add(new()
                    {
                        ProductId = item.ProductId,
                        ProductName = _products.FirstOrDefault(s => s.Id == item.ProductId)?.Name ?? "",
                        Quantity = item.Quantity,
                        Rate = item.Rate,
                        Total = item.Total,
                        Remarks = item.Remarks
                    });
                }
            }

            else if (await DataStorageService.LocalExists(StorageFileNames.KitchenProductionCartDataFileName))
                _cart = System.Text.Json.JsonSerializer.Deserialize<List<KitchenProductionProductCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.KitchenProductionCartDataFileName));
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
        _kitchenProduction.CompanyId = _selectedCompany.Id;

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
        _kitchenProduction.KitchenId = _selectedKitchen.Id;

        await LoadItems();
        await SaveTransactionFile();
    }

    private async Task OnTransactionDateChanged(Syncfusion.Blazor.Calendars.ChangedEventArgs<DateTime> args)
    {
        _kitchenProduction.TransactionDateTime = args.Value;
        await LoadItems();
        await SaveTransactionFile();
    }
    #endregion

    #region Cart
    private async Task OnItemChanged(ChangeEventArgs<ProductModel?, ProductModel> args)
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
                ProductId = 0,
                ProductName = "",
                Quantity = 1,
                Rate = 0
            };

        else
        {
            _selectedCart.ProductId = _selectedProduct.Id;
            _selectedCart.ProductName = _selectedProduct.Name;
            _selectedCart.Quantity = 1;
            _selectedCart.Rate = _selectedProduct.Rate;
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
        if (_selectedProduct is null)
            return;

        if (_selectedCart.Quantity <= 0)
            _selectedCart.Quantity = 1;

        _selectedCart.ProductId = _selectedProduct.Id;
        _selectedCart.ProductName = _selectedProduct.Name;
        _selectedCart.Total = _selectedProduct.Rate * _selectedCart.Quantity;

        StateHasChanged();
    }

    private async Task AddItemToCart()
    {
        if (_selectedProduct is null || _selectedProduct.Id <= 0 || _selectedCart.Quantity <= 0 || _selectedCart.Rate < 0 || _selectedCart.Total < 0)
        {
            await ShowToast("Invalid Product Details", "Please ensure all product details are correctly filled before adding to the cart.", "error");
            return;
        }

        UpdateSelectedItemFinancialDetails();

        var existingItem = _cart.FirstOrDefault(s => s.ProductId == _selectedCart.ProductId);
        if (existingItem is not null)
        {
            existingItem.Quantity += _selectedCart.Quantity;
            existingItem.Rate = _selectedCart.Rate;
        }
        else
            _cart.Add(new()
            {
                ProductId = _selectedCart.ProductId,
                ProductName = _selectedCart.ProductName,
                Quantity = _selectedCart.Quantity,
                Rate = _selectedCart.Rate,
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

	private async Task EditCartItem(KitchenProductionProductCartModel cartItem)
    {
        _selectedProduct = _products.FirstOrDefault(s => s.Id == cartItem.ProductId);

        if (_selectedProduct is null)
            return;

        _selectedCart = new()
        {
            ProductId = cartItem.ProductId,
            ProductName = cartItem.ProductName,
            Quantity = cartItem.Quantity,
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

	private async Task RemoveItemFromCart(KitchenProductionProductCartModel cartItem)
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

        _kitchenProduction.TotalItems = _cart.Count;
        _kitchenProduction.TotalQuantity = _cart.Sum(x => x.Quantity);
        _kitchenProduction.TotalAmount = _cart.Sum(x => x.Total);
        _itemAfterTaxTotal = _cart.Sum(x => x.Total);

        _kitchenProduction.CompanyId = _selectedCompany.Id;
        _kitchenProduction.KitchenId = _selectedKitchen.Id;
        _kitchenProduction.CreatedBy = _user.Id;

        #region Financial Year
        _selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_kitchenProduction.TransactionDateTime);
        if (_selectedFinancialYear is not null && !_selectedFinancialYear.Locked)
            _kitchenProduction.FinancialYearId = _selectedFinancialYear.Id;
        else
        {
            await ShowToast("Invalid Transaction Date", "The selected transaction date does not fall within an active financial year.", "error");
            _kitchenProduction.TransactionDateTime = await CommonData.LoadCurrentDateTime();
            _selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_kitchenProduction.TransactionDateTime);
            _kitchenProduction.FinancialYearId = _selectedFinancialYear.Id;
        }
        #endregion

        if (Id is null)
            _kitchenProduction.TransactionNo = await GenerateCodes.GenerateKitchenProductionTransactionNo(_kitchenProduction);
    }

    private async Task SaveTransactionFile()
    {
        if (_isProcessing || _isLoading)
            return;

        try
        {
            _isProcessing = true;

            await UpdateFinancialDetails();

            await DataStorageService.LocalSaveAsync(StorageFileNames.KitchenProductionDataFileName, System.Text.Json.JsonSerializer.Serialize(_kitchenProduction));
            await DataStorageService.LocalSaveAsync(StorageFileNames.KitchenProductionCartDataFileName, System.Text.Json.JsonSerializer.Serialize(_cart));
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
        if (_selectedCompany is null || _kitchenProduction.CompanyId <= 0)
        {
            await ShowToast("Company Not Selected", "Please select a company for the transaction.", "error");
            return false;
        }

        if (_selectedKitchen is null || _kitchenProduction.KitchenId <= 0)
        {
            await ShowToast("Kitchen Not Selected", "Please select a kitchen for the transaction.", "error");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_kitchenProduction.TransactionNo))
        {
            await ShowToast("Transaction Number Missing", "Please enter a transaction number for the transaction.", "error");
            return false;
        }

        if (_kitchenProduction.TransactionDateTime == default)
        {
            await ShowToast("Transaction Date Missing", "Please select a valid transaction date for the transaction.", "error");
            return false;
        }

        if (_selectedFinancialYear is null || _kitchenProduction.FinancialYearId <= 0)
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

        if (_kitchenProduction.TotalItems <= 0)
        {
            await ShowToast("No Items in Cart", "The transaction must contain at least one item in the cart.", "error");
            return false;
        }

        if (_kitchenProduction.TotalQuantity <= 0)
        {
            await ShowToast("Invalid Total Quantity", "The total quantity of the transaction must be greater than zero.", "error");
            return false;
        }

        if (_kitchenProduction.TotalAmount < 0)
        {
            await ShowToast("Invalid Total Amount", "The total amount of the transaction must be greater than zero.", "error");
            return false;
        }

        if (_cart.Any(item => item.Quantity <= 0))
        {
            await ShowToast("Invalid Product Quantity", "One or more products in the cart have a quantity less than or equal to zero. Please correct the quantities before saving.", "error");
            return false;
        }

        if (_kitchenProduction.Id > 0)
        {
            var existingKitchenProduction = await CommonData.LoadTableDataById<KitchenProductionModel>(TableNames.KitchenProduction, _kitchenProduction.Id);
            var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, _kitchenProduction.FinancialYearId);
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

        _kitchenProduction.Remarks = _kitchenProduction.Remarks?.Trim();
        if (string.IsNullOrWhiteSpace(_kitchenProduction.Remarks))
            _kitchenProduction.Remarks = null;

        return true;
    }

    private async Task SaveTransaction()
    {
        if (_isProcessing || _isLoading)
            return;

        try
        {
            _isProcessing = true;
            await ShowToast("Processing Transaction", "Please wait while the transaction is being saved...", "success");

            await SaveTransactionFile();

            if (!await ValidateForm())
            {
                _isProcessing = false;
                return;
            }

            _kitchenProduction.Status = true;
            var currentDateTime = await CommonData.LoadCurrentDateTime();
            _kitchenProduction.TransactionDateTime = DateOnly.FromDateTime(_kitchenProduction.TransactionDateTime).ToDateTime(new TimeOnly(currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second));
            _kitchenProduction.LastModifiedAt = currentDateTime;
            _kitchenProduction.CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
            _kitchenProduction.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
            _kitchenProduction.CreatedBy = _user.Id;
            _kitchenProduction.LastModifiedBy = _user.Id;

            _kitchenProduction.Id = await KitchenProductionData.SaveKitchenProductionTransaction(_kitchenProduction, _cart);
            var (pdfStream, fileName) = await KitchenProductionData.GenerateAndDownloadInvoice(_kitchenProduction.Id);
            await SaveAndViewService.SaveAndView(fileName, pdfStream);
            await DeleteLocalFiles();
            NavigationManager.NavigateTo(PageRouteNames.KitchenProduction, true);

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
        await DataStorageService.LocalRemove(StorageFileNames.KitchenProductionDataFileName);
        await DataStorageService.LocalRemove(StorageFileNames.KitchenProductionCartDataFileName);
    }
    #endregion

    #region Utilities
    private async Task ResetPage()
    {
        await DeleteLocalFiles();
        NavigationManager.NavigateTo(PageRouteNames.KitchenProduction, true);
    }

    private async Task NavigateToTransactionHistoryPage()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportKitchenProduction, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ReportKitchenProduction);
    }

    private async Task NavigateToItemReport()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportKitchenProductionItem, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ReportKitchenProductionItem);
    }

    private async Task DownloadInvoice()
    {
        if (!Id.HasValue || Id.Value <= 0)
        {
            await ShowToast("No Transaction Selected", "Please save the transaction first before downloading the invoice.", "error");
            return;
        }

        if (_isProcessing)
            return;

        try
        {
            _isProcessing = true;
            var (pdfStream, fileName) = await KitchenProductionData.GenerateAndDownloadInvoice(Id.Value);
            await SaveAndViewService.SaveAndView(fileName, pdfStream);
            await ShowToast("Invoice Downloaded", "The invoice has been downloaded successfully.", "success");
        }
        catch (Exception ex)
        {
            await ShowToast("An Error Occurred While Downloading Invoice", ex.Message, "error");
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

    public async ValueTask DisposeAsync()
    {
        if (_hotKeysContext is not null)
            await _hotKeysContext.DisposeAsync();
    }
    #endregion
}