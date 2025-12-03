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
using Syncfusion.Blazor.Notifications;

namespace PrimeBakes.Shared.Pages.Sales.Order;

public partial class OrderPage : IAsyncDisposable
{
    private HotKeysContext _hotKeysContext;

	[Parameter] public int? Id { get; set; }

    private UserModel _user;

    private bool _isLoading = true;
    private bool _isProcessing = false;

    private CompanyModel _selectedCompany = new();
    private LocationModel _selectedLocation = new();
    private FinancialYearModel _selectedFinancialYear = new();
    private ProductLocationOverviewModel? _selectedProduct = new();
    private OrderItemCartModel _selectedCart = new();
    private OrderModel _order = new();
    private SaleModel _sale = new();

    private List<CompanyModel> _companies = [];
    private List<LocationModel> _locations = [];
    private List<ProductLocationOverviewModel> _products = [];
    private List<OrderItemCartModel> _cart = [];

    private SfAutoComplete<ProductLocationOverviewModel?, ProductLocationOverviewModel> _sfItemAutoComplete;
    private SfGrid<OrderItemCartModel> _sfCartGrid;

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

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Order);
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

		await LoadLocations();
        await LoadCompanies();
        await LoadExistingOrder();
        await LoadItems();
        await LoadExistingCart();
        await SaveOrderFile();
    }

    private async Task LoadLocations()
    {
        try
        {
            _locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location);
            _locations.RemoveAll(s => s.Id == 1);
            _locations = [.. _locations.OrderBy(s => s.Name)];
            _locations.Add(new()
            {
                Id = 0,
                Name = "Create New Location ..."
            });

            if (_user.LocationId > 1)
                _selectedLocation = _locations.FirstOrDefault(s => s.Id == _user.LocationId);
            else
                _selectedLocation = _locations.FirstOrDefault();
        }
        catch (Exception ex)
        {
            await ShowToast("An Error Occurred While Loading Locations", ex.Message, "error");
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
            await ShowToast("An Error Occurred While Loading Companies", ex.Message, "error");
        }
    }

    private async Task LoadExistingOrder()
    {
        try
        {
            if (Id.HasValue)
            {
                _order = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, Id.Value);
                if (_order is null || _order.Id == 0 || _user.LocationId > 1)
                {
					await ShowToast("Transaction Not Found", "The requested transaction could not be found.", "error");
                    NavigationManager.NavigateTo(PageRouteNames.Order, true);
                }
            }

            else if (await DataStorageService.LocalExists(StorageFileNames.OrderDataFileName))
                _order = System.Text.Json.JsonSerializer.Deserialize<OrderModel>(await DataStorageService.LocalGetAsync(StorageFileNames.OrderDataFileName));

            else
            {
                _order = new()
                {
                    Id = 0,
                    TransactionNo = string.Empty,
                    CompanyId = _selectedCompany.Id,
                    LocationId = _user.LocationId > 1 ? _user.LocationId : _locations.FirstOrDefault().Id,
                    TransactionDateTime = await CommonData.LoadCurrentDateTime(),
                    FinancialYearId = (await FinancialYearData.LoadFinancialYearByDateTime(await CommonData.LoadCurrentDateTime())).Id,
                    CreatedBy = _user.Id,
                    SaleId = null,
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
                _selectedLocation = _locations.FirstOrDefault(s => s.Id == _order.LocationId);
            else
            {
                _selectedLocation = _locations.FirstOrDefault(s => s.Id == _user.LocationId);
                _order.LocationId = _selectedLocation.Id;

                _order.TransactionDateTime = await CommonData.LoadCurrentDateTime();
            }

            if (_order.CompanyId > 0 && _user.LocationId == 1)
                _selectedCompany = _companies.FirstOrDefault(s => s.Id == _order.CompanyId);
            else
            {
                var mainCompanyId = await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId);
                _selectedCompany = _companies.FirstOrDefault(s => s.Id.ToString() == mainCompanyId.Value);
                _order.CompanyId = _selectedCompany.Id;
            }

            if (_order.SaleId is not null && _order.SaleId > 0)
                _sale = await CommonData.LoadTableDataById<SaleModel>(TableNames.Sale, _order.SaleId.Value);

            _selectedFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, _order.FinancialYearId);
        }
        catch (Exception ex)
        {
			await ShowToast("An Error Occurred While Loading Transaction Data", ex.Message, "error");
            await DeleteLocalFiles();
        }
        finally
        {
            await SaveOrderFile();
        }
    }

    private async Task LoadItems()
    {
        try
        {
            _products = await ProductData.LoadProductByLocation(_order.LocationId);
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
            await ShowToast("An Error Occurred While Loading Items", ex.Message, "error");
        }
    }

    private async Task LoadExistingCart()
    {
        try
        {
            _cart.Clear();

            if (_order.Id > 0)
            {
                var existingCart = await CommonData.LoadTableDataByMasterId<OrderDetailModel>(TableNames.OrderDetail, _order.Id);

                foreach (var item in existingCart)
                {
                    if (_products.FirstOrDefault(s => s.ProductId == item.ProductId) is null)
                    {
						var product = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, item.ProductId);
						await ShowToast("Item Not Found", $"The item {product?.Name} (ID: {item.ProductId}) in the existing transaction cart was not found in the available items list. It may have been deleted or is inaccessible.", "error");
                        continue;
                    }

                    _cart.Add(new()
                    {
                        ItemId = item.ProductId,
                        ItemName = _products.FirstOrDefault(s => s.ProductId == item.ProductId)?.Name ?? "",
                        Quantity = item.Quantity,
                        Remarks = item.Remarks
                    });
                }
            }

            else if (await DataStorageService.LocalExists(StorageFileNames.OrderCartDataFileName))
                _cart = System.Text.Json.JsonSerializer.Deserialize<List<OrderItemCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.OrderCartDataFileName));
        }
        catch (Exception ex)
        {
            await ShowToast("An Error Occurred While Loading Existing Cart", ex.Message, "error");
            await DeleteLocalFiles();
        }
        finally
        {
            await SaveOrderFile();
        }
    }
    #endregion

    #region Change Events
    private async Task OnLocationChanged(ChangeEventArgs<LocationModel, LocationModel> args)
    {
        if (_user.LocationId > 1)
        {
            _selectedLocation = _locations.FirstOrDefault(s => s.Id == _user.LocationId);
            _order.LocationId = _selectedLocation.Id;
            await ShowToast("Location Change Not Allowed", "You are not allowed to change the location.", "error");
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
        _order.LocationId = _selectedLocation.Id;

        await LoadItems();
        await SaveOrderFile();
    }

    private async Task OnCompanyChanged(ChangeEventArgs<CompanyModel, CompanyModel> args)
    {
        if (_user.LocationId > 1)
        {
            var mainCompanyId = await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId);
            _selectedCompany = _companies.FirstOrDefault(s => s.Id.ToString() == mainCompanyId.Value);
            _order.CompanyId = _selectedCompany.Id;
            await ShowToast("Company Change Not Allowed", "You are not allowed to change the company.", "error");
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
        _order.CompanyId = _selectedCompany.Id;

        await SaveOrderFile();
    }

    private async Task OnTransactionDateChanged(Syncfusion.Blazor.Calendars.ChangedEventArgs<DateTime> args)
    {
        if (_user.LocationId > 1)
        {
            _order.TransactionDateTime = await CommonData.LoadCurrentDateTime();
            await ShowToast("Transaction Date Change Not Allowed", "You are not allowed to change the transaction date.", "error");
            return;
        }

        _order.TransactionDateTime = args.Value;
        await LoadItems();
        await SaveOrderFile();
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
                ItemCategoryId = 0,
                ItemId = 0,
                ItemName = "",
                Quantity = 1,
            };

        else
        {
            _selectedCart.ItemCategoryId = _selectedProduct.ProductCategoryId;
            _selectedCart.ItemId = _selectedProduct.ProductId;
            _selectedCart.ItemName = _selectedProduct.Name;
            _selectedCart.Quantity = 1;
        }

        UpdateSelectedItemFinancialDetails();
    }

    private void OnItemQuantityChanged(ChangeEventArgs<decimal> args)
    {
        _selectedCart.Quantity = args.Value;
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

        StateHasChanged();
    }

    private async Task AddItemToCart()
    {
        if (_selectedProduct is null || _selectedProduct.ProductId <= 0 || _selectedCart.Quantity <= 0)
        {
            await ShowToast("Invalid Item Details", "Please ensure all item details are correctly filled before adding to the cart.", "error");
            return;
        }

        UpdateSelectedItemFinancialDetails();

        var existingItem = _cart.FirstOrDefault(s => s.ItemId == _selectedCart.ItemId);
        if (existingItem is not null)
            existingItem.Quantity += _selectedCart.Quantity;
        else
            _cart.Add(new()
            {
                ItemCategoryId = _selectedCart.ItemCategoryId,
                ItemId = _selectedCart.ItemId,
                ItemName = _selectedCart.ItemName,
                Quantity = _selectedCart.Quantity,
                Remarks = _selectedCart.Remarks
            });

        _selectedProduct = null;
        _selectedCart = new();

        await _sfItemAutoComplete.FocusAsync();
        await SaveOrderFile();
    }

	private async Task EditSelectedCartItem()
	{
		if (_sfCartGrid is null || _sfCartGrid.SelectedRecords is null || _sfCartGrid.SelectedRecords.Count == 0)
			return;

		var selectedCartItem = _sfCartGrid.SelectedRecords.First();
		await EditCartItem(selectedCartItem);
	}

	private async Task EditCartItem(OrderItemCartModel cartItem)
    {
        _selectedProduct = _products.FirstOrDefault(s => s.ProductId == cartItem.ItemId);

        if (_selectedProduct is null)
            return;

        _selectedCart = new()
        {
            ItemCategoryId = cartItem.ItemCategoryId,
            ItemId = cartItem.ItemId,
            ItemName = cartItem.ItemName,
            Quantity = cartItem.Quantity,
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

	private async Task RemoveItemFromCart(OrderItemCartModel cartItem)
    {
        _cart.Remove(cartItem);
        await SaveOrderFile();
    }
    #endregion

    #region Saving
    private async Task UpdateFinancialDetails()
    {
        foreach (var item in _cart)
        {
            if (item.Quantity == 0)
                _cart.Remove(item);

            item.Remarks = item.Remarks?.Trim();
            if (string.IsNullOrWhiteSpace(item.Remarks))
                item.Remarks = null;
        }

        _order.TotalItems = _cart.Count;
        _order.TotalQuantity = _cart.Sum(s => s.Quantity);

		var mainCompanyId = await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId);
        _order.CompanyId = _user.LocationId == 1 ? _selectedCompany.Id : _companies.FirstOrDefault(s => s.Id.ToString() == mainCompanyId.Value).Id;
        _order.LocationId = _user.LocationId == 1 ? _selectedLocation.Id : _user.LocationId;
        _order.CreatedBy = _user.Id;
        _order.TransactionDateTime = _user.LocationId == 1 ? _order.TransactionDateTime : await CommonData.LoadCurrentDateTime();

        #region Financial Year
        _selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_order.TransactionDateTime);
        if (_selectedFinancialYear is not null && !_selectedFinancialYear.Locked)
            _order.FinancialYearId = _selectedFinancialYear.Id;
        else
        {
            await ShowToast("Invalid Transaction Date", "The selected transaction date does not fall within an active financial year.", "error");
            _order.TransactionDateTime = await CommonData.LoadCurrentDateTime();
            _selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_order.TransactionDateTime);
            _order.FinancialYearId = _selectedFinancialYear.Id;
        }
        #endregion

        if (Id is null)
            _order.TransactionNo = await GenerateCodes.GenerateOrderTransactionNo(_order);

        if (_order.SaleId is not null && _order.SaleId > 0)
            _sale = await CommonData.LoadTableDataById<SaleModel>(TableNames.Sale, _order.SaleId.Value);
    }

    private async Task SaveOrderFile()
    {
        if (_isProcessing || _isLoading)
            return;

        try
        {
            _isProcessing = true;

            await UpdateFinancialDetails();

            await DataStorageService.LocalSaveAsync(StorageFileNames.OrderDataFileName, System.Text.Json.JsonSerializer.Serialize(_order));
            await DataStorageService.LocalSaveAsync(StorageFileNames.OrderCartDataFileName, System.Text.Json.JsonSerializer.Serialize(_cart));
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
        if (_user.LocationId > 1)
        {
            _order.LocationId = _user.LocationId;
            _selectedLocation = _locations.FirstOrDefault(s => s.Id == _user.LocationId);

            var mainCompanyId = await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId);
            _order.CompanyId = _companies.FirstOrDefault(s => s.Id.ToString() == mainCompanyId.Value).Id;
            _selectedCompany = _companies.FirstOrDefault(s => s.Id.ToString() == mainCompanyId.Value);

            _order.TransactionDateTime = await CommonData.LoadCurrentDateTime();
        }

        if (_order.LocationId <= 1)
        {
            await ShowToast("Location Not Selected", "Please select a location for the transaction.", "error");
            return false;
        }

        if (_selectedCompany is null || _order.CompanyId <= 0)
        {
            await ShowToast("Company Not Selected", "Please select a company for the transaction.", "error");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_order.TransactionNo))
        {
            await ShowToast("Transaction Number Missing", "Please enter a transaction number for the transaction.", "error");
            return false;
        }

        if (_order.TransactionDateTime == default)
        {
            await ShowToast("Transaction Date Missing", "Please select a valid transaction date for the transaction.", "error");
            return false;
        }

        if (_selectedFinancialYear is null || _order.FinancialYearId <= 0)
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

		if (_order.TotalItems <= 0)
		{
			await ShowToast("Cart is Empty", "Please add at least one item to the cart before saving the transaction.", "error");
			return false;
		}

		if (_order.TotalQuantity <= 0)
		{
			await ShowToast("Invalid Total Quantity", "The total quantity of items in the cart must be greater than zero.", "error");
			return false;
		}

		if (_cart.Any(item => item.Quantity <= 0))
        {
            await ShowToast("Invalid Item Quantity", "One or more items in the cart have a quantity less than or equal to zero. Please correct the quantities before saving.", "error");
            return false;
        }

        if (_order.Id > 0)
        {
            var existingOrder = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, _order.Id);
            var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, existingOrder.FinancialYearId);
            if (financialYear is null || financialYear.Locked || financialYear.Status == false)
            {
                await ShowToast("Financial Year Locked or Inactive", "The financial year for the selected transaction date is either locked or inactive. Please select a different date.", "error");
                return false;
            }

            if (existingOrder.SaleId is not null && existingOrder.SaleId > 0)
            {
                await ShowToast("Order Already Converted to Sale", "This order has already been converted to a sale and cannot be modified.", "error");
                return false;
            }

            if (!_user.Admin || _user.LocationId > 1)
            {
                await ShowToast("Insufficient Permissions", "You do not have the necessary permissions to modify this transaction.", "error");
                await DeleteLocalFiles();
                NavigationManager.NavigateTo(PageRouteNames.Order, true);
                return false;
            }
        }

        _order.Remarks = _order.Remarks?.Trim();
        if (string.IsNullOrWhiteSpace(_order.Remarks))
            _order.Remarks = null;

        return true;
    }

    private async Task SaveTransaction()
    {
        if (_isProcessing || _isLoading)
            return;

        try
        {
            _isProcessing = true;

            await SaveOrderFile();

            if (!await ValidateForm())
            {
                _isProcessing = false;
                return;
            }

            _order.Status = true;
            var currentDateTime = await CommonData.LoadCurrentDateTime();
            _order.TransactionDateTime = DateOnly.FromDateTime(_order.TransactionDateTime).ToDateTime(new TimeOnly(currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second));
            _order.LastModifiedAt = currentDateTime;
            _order.CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
            _order.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
            _order.CreatedBy = _user.Id;
            _order.LastModifiedBy = _user.Id;

            _order.Id = await OrderData.SaveOrderTransaction(_order, _cart);
            var (pdfStream, fileName) = await OrderData.GenerateAndDownloadInvoice(_order.Id);
            await SaveAndViewService.SaveAndView(fileName, pdfStream);
            await DeleteLocalFiles();
            NavigationManager.NavigateTo(PageRouteNames.Order, true);

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
        await DataStorageService.LocalRemove(StorageFileNames.OrderDataFileName);
        await DataStorageService.LocalRemove(StorageFileNames.OrderCartDataFileName);
    }
	#endregion

	#region Utilities
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
			var (pdfStream, fileName) = await OrderData.GenerateAndDownloadInvoice(Id.Value);
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

	private async Task ResetPage()
    {
        await DeleteLocalFiles();
        NavigationManager.NavigateTo(PageRouteNames.Order, true);
    }

    private async Task NavigateToTransactionHistoryPage()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportOrder, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ReportOrder);
    }

	private async Task NavigateToItemReport()
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportOrderItem, "_blank");
		else
			NavigationManager.NavigateTo(PageRouteNames.ReportOrderItem);
	}

	private async Task NavigateToSelectedSalePage()
    {
        if (_order.SaleId is null || _order.SaleId <= 0)
        {
            await ShowToast("No Sale Linked", "There is no sale linked to this order to view.", "error");
            return;

        }
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", $"{PageRouteNames.Sale}/{_order.SaleId}", "_blank");
        else
            NavigationManager.NavigateTo($"{PageRouteNames.Sale}/{_order.SaleId}");
    }

    private async Task DownloadSelectedSale()
    {
        if (_order.SaleId is null || _order.SaleId <= 0)
        {
            await ShowToast("No Sale Linked", "There is no sale linked to this order to download.", "error");
            return;
        }

        var (fileStream, fileName) = await SaleData.GenerateAndDownloadInvoice(_order.SaleId.Value);
        await SaveAndViewService.SaveAndView(fileName, fileStream);
    }

	private async Task NavigateToDashboard() =>
		NavigationManager.NavigateTo(PageRouteNames.Dashboard);

	private async Task NavigateBack() =>
		NavigationManager.NavigateTo(PageRouteNames.SalesDashboard);

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