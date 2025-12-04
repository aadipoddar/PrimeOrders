using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakes.Shared.Components;

using PrimeBakesLibrary.Data;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Purchase;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Inventory.Purchase;
using PrimeBakesLibrary.Models.Sales.Product;

using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Inputs;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Inventory.Purchase;

public partial class PurchaseReturnPage : IAsyncDisposable
{
    private HotKeysContext _hotKeysContext;

    [Parameter] public int? Id { get; set; }

    private UserModel _user;

    private bool _isLoading = true;
    private bool _isProcessing = false;
    private bool _autoGenerateTransactionNo = false;
    private bool _isUploadDialogVisible = false;

    private CompanyModel _selectedCompany = new();
    private LedgerModel _selectedParty = new();
    private FinancialYearModel _selectedFinancialYear = new();
    private RawMaterialModel? _selectedRawMaterial = new();
    private PurchaseReturnItemCartModel _selectedCart = new();
    private PurchaseReturnModel _purchaseReturn = new();

    private List<CompanyModel> _companies = [];
    private List<LedgerModel> _parties = [];
    private List<RawMaterialModel> _rawMaterials = [];
    private List<TaxModel> _taxes = [];
    private List<PurchaseReturnItemCartModel> _cart = [];

    private SfAutoComplete<RawMaterialModel?, RawMaterialModel> _sfItemAutoComplete;
    private SfGrid<PurchaseReturnItemCartModel> _sfCartGrid;
    private SfDialog _uploadDocumentDialog;
    private SfUploader _sfDocumentUploader;

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
            .Add(ModCode.Ctrl, Code.U, UploadDocument, "Upload document", Exclude.None)
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
        await LoadLedgers();
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

            _selectedParty = _parties.FirstOrDefault();
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
                _purchaseReturn = await CommonData.LoadTableDataById<PurchaseReturnModel>(TableNames.PurchaseReturn, Id.Value);
                if (_purchaseReturn is null)
                {
                    await _toastNotification.ShowAsync("Transaction Not Found", "The requested transaction could not be found.", ToastType.Error);
                    NavigationManager.NavigateTo(PageRouteNames.PurchaseReturn, true);
                }
            }

            else if (await DataStorageService.LocalExists(StorageFileNames.PurchaseReturnDataFileName))
                _purchaseReturn = System.Text.Json.JsonSerializer.Deserialize<PurchaseReturnModel>(await DataStorageService.LocalGetAsync(StorageFileNames.PurchaseReturnDataFileName));

            else
            {
                _purchaseReturn = new()
                {
                    Id = 0,
                    TransactionNo = string.Empty,
                    CompanyId = _selectedCompany.Id,
                    PartyId = _selectedParty.Id,
                    TransactionDateTime = await CommonData.LoadCurrentDateTime(),
                    FinancialYearId = (await FinancialYearData.LoadFinancialYearByDateTime(await CommonData.LoadCurrentDateTime())).Id,
                    CreatedBy = _user.Id,
                    BaseTotal = 0,
                    TotalQuantity = 0,
                    DocumentUrl = null,
                    TotalItems = 0,
                    ItemDiscountAmount = 0,
                    TotalAfterItemDiscount = 0,
                    TotalInclusiveTaxAmount = 0,
                    TotalExtraTaxAmount = 0,
                    TotalAfterTax = 0,
                    CashDiscountPercent = 0,
                    CashDiscountAmount = 0,
                    OtherChargesPercent = 0,
                    OtherChargesAmount = 0,
                    RoundOffAmount = 0,
                    TotalAmount = 0,
                    Remarks = null,
                    CreatedAt = DateTime.Now,
                    CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform(),
                    Status = true,
                    LastModifiedAt = null,
                    LastModifiedBy = null,
                    LastModifiedFromPlatform = null
                };
                await DeleteLocalFiles();
            }

            if (_purchaseReturn.CompanyId > 0)
                _selectedCompany = _companies.FirstOrDefault(s => s.Id == _purchaseReturn.CompanyId);
            else
            {
                _selectedCompany = _companies.FirstOrDefault();
                _purchaseReturn.CompanyId = _selectedCompany.Id;
            }

            if (_purchaseReturn.PartyId > 0)
                _selectedParty = _parties.FirstOrDefault(s => s.Id == _purchaseReturn.PartyId);
            else
            {
                _selectedParty = _parties.FirstOrDefault();
                _purchaseReturn.PartyId = _selectedParty.Id;
            }

            _selectedFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, _purchaseReturn.FinancialYearId);
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
            _rawMaterials = await PurchaseData.LoadRawMaterialByPartyPurchaseDateTime(_purchaseReturn.PartyId, _purchaseReturn.TransactionDateTime);
            _taxes = await CommonData.LoadTableDataByStatus<TaxModel>(TableNames.Tax);

            _rawMaterials = [.. _rawMaterials.OrderBy(s => s.Name)];
            _rawMaterials.Add(new()
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

            if (_purchaseReturn.Id > 0)
            {
                var existingCart = await CommonData.LoadTableDataByMasterId<PurchaseReturnDetailModel>(TableNames.PurchaseReturnDetail, _purchaseReturn.Id);

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

            else if (await DataStorageService.LocalExists(StorageFileNames.PurchaseReturnCartDataFileName))
                _cart = System.Text.Json.JsonSerializer.Deserialize<List<PurchaseReturnItemCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.PurchaseReturnCartDataFileName));
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
        _purchaseReturn.CompanyId = _selectedCompany.Id;

        await SaveTransactionFile();
    }

    private async Task OnPartyChanged(ChangeEventArgs<LedgerModel, LedgerModel> args)
    {
        if (args.Value is null)
            return;

        if (args.Value.Id == 0)
        {
            if (FormFactor.GetFormFactor() == "Web")
                await JSRuntime.InvokeVoidAsync("open", PageRouteNames.AdminLedger, "_blank");
            else
                NavigationManager.NavigateTo(PageRouteNames.AdminLedger);

            return;
        }

        _selectedParty = args.Value;
        _purchaseReturn.PartyId = _selectedParty.Id;

        await LoadItems();
        await SaveTransactionFile();
    }

    private async Task OnTransactionDateChanged(Syncfusion.Blazor.Calendars.ChangedEventArgs<DateTime> args)
    {
        _purchaseReturn.TransactionDateTime = args.Value;
        await LoadItems();
        await SaveTransactionFile();
    }

    private async Task OnAutoGenerateTransactionNoChecked(Syncfusion.Blazor.Buttons.ChangeEventArgs<bool> args)
    {
        _autoGenerateTransactionNo = args.Checked;
        await SaveTransactionFile();
    }

    private async Task OnCashDiscountPercentChanged(ChangeEventArgs<decimal> args)
    {
        _purchaseReturn.CashDiscountPercent = args.Value;
        await SaveTransactionFile();
    }

    private async Task OnOtherDiscountPercentChanged(ChangeEventArgs<decimal> args)
    {
        _purchaseReturn.OtherChargesPercent = args.Value;
        await SaveTransactionFile();
    }

    private async Task OnRoundOffAmountChanged(ChangeEventArgs<decimal> args)
    {
        _purchaseReturn.RoundOffAmount = args.Value;
        await SaveTransactionFile(true);
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
                Rate = 0,
                DiscountPercent = 0,
                CGSTPercent = 0,
                SGSTPercent = 0,
                IGSTPercent = 0
            };

        else
        {
            var isSameState = _selectedParty.StateUTId == _selectedCompany.StateUTId;

            _selectedCart.ItemId = _selectedRawMaterial.Id;
            _selectedCart.ItemName = _selectedRawMaterial.Name;
            _selectedCart.Quantity = 1;
            _selectedCart.UnitOfMeasurement = _selectedRawMaterial.UnitOfMeasurement;
            _selectedCart.Rate = _selectedRawMaterial.Rate;
            _selectedCart.DiscountPercent = 0;
            _selectedCart.CGSTPercent = _taxes.FirstOrDefault(s => s.Id == _selectedRawMaterial.TaxId).CGST;
            _selectedCart.SGSTPercent = isSameState ? _taxes.FirstOrDefault(s => s.Id == _selectedRawMaterial.TaxId).SGST : 0;
            _selectedCart.IGSTPercent = isSameState ? 0 : _taxes.FirstOrDefault(s => s.Id == _selectedRawMaterial.TaxId).IGST;
            _selectedCart.InclusiveTax = _taxes.FirstOrDefault(s => s.Id == _selectedRawMaterial.TaxId).Inclusive;
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
        if (_selectedRawMaterial is null)
            return;

        if (_selectedCart.Quantity <= 0)
            _selectedCart.Quantity = 1;

        if (string.IsNullOrWhiteSpace(_selectedCart.UnitOfMeasurement))
            _selectedCart.UnitOfMeasurement = _selectedRawMaterial.UnitOfMeasurement;

        _selectedCart.ItemId = _selectedRawMaterial.Id;
        _selectedCart.ItemName = _selectedRawMaterial.Name;
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
        if (_selectedRawMaterial is null || _selectedRawMaterial.Id <= 0 || _selectedCart.Quantity <= 0 || _selectedCart.Rate < 0 || _selectedCart.DiscountPercent < 0 || _selectedCart.CGSTPercent < 0 || _selectedCart.SGSTPercent < 0 || _selectedCart.IGSTPercent < 0 || _selectedCart.Total < 0 || string.IsNullOrEmpty(_selectedCart.UnitOfMeasurement))
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
                UnitOfMeasurement = _selectedCart.UnitOfMeasurement,
                Rate = _selectedCart.Rate,
                DiscountPercent = _selectedCart.DiscountPercent,
                CGSTPercent = _selectedCart.CGSTPercent,
                SGSTPercent = _selectedCart.SGSTPercent,
                IGSTPercent = _selectedCart.IGSTPercent,
                InclusiveTax = _selectedCart.InclusiveTax,
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

    private async Task EditCartItem(PurchaseReturnItemCartModel cartItem)
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

    private async Task RemoveItemFromCart(PurchaseReturnItemCartModel cartItem)
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
            var withOtherCharges = perUnitCost * (1 + _purchaseReturn.OtherChargesPercent / 100);
            item.NetRate = withOtherCharges * (1 - _purchaseReturn.CashDiscountPercent / 100);

            item.Remarks = item.Remarks?.Trim();
            if (string.IsNullOrWhiteSpace(item.Remarks))
                item.Remarks = null;
        }

        _purchaseReturn.TotalItems = _cart.Count;
        _purchaseReturn.TotalQuantity = _cart.Sum(x => x.Quantity);
        _purchaseReturn.BaseTotal = _cart.Sum(x => x.BaseTotal);
        _purchaseReturn.ItemDiscountAmount = _cart.Sum(x => x.DiscountAmount);
        _purchaseReturn.TotalAfterItemDiscount = _cart.Sum(x => x.AfterDiscount);
        _purchaseReturn.TotalInclusiveTaxAmount = _cart.Where(x => x.InclusiveTax).Sum(x => x.TotalTaxAmount);
        _purchaseReturn.TotalExtraTaxAmount = _cart.Where(x => !x.InclusiveTax).Sum(x => x.TotalTaxAmount);
        _purchaseReturn.TotalAfterTax = _cart.Sum(x => x.Total);

        _purchaseReturn.OtherChargesAmount = _purchaseReturn.TotalAfterTax * _purchaseReturn.OtherChargesPercent / 100;
        var totalAfterOtherCharges = _purchaseReturn.TotalAfterTax + _purchaseReturn.OtherChargesAmount;

        _purchaseReturn.CashDiscountAmount = totalAfterOtherCharges * _purchaseReturn.CashDiscountPercent / 100;
        var totalAfterCashDiscount = totalAfterOtherCharges - _purchaseReturn.CashDiscountAmount;

        if (!customRoundOff)
            _purchaseReturn.RoundOffAmount = Math.Round(totalAfterCashDiscount) - totalAfterCashDiscount;

        _purchaseReturn.TotalAmount = totalAfterCashDiscount + _purchaseReturn.RoundOffAmount;

        _purchaseReturn.CompanyId = _selectedCompany.Id;
        _purchaseReturn.PartyId = _selectedParty.Id;
        _purchaseReturn.CreatedBy = _user.Id;

        #region Financial Year
        _selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_purchaseReturn.TransactionDateTime);
        if (_selectedFinancialYear is not null && !_selectedFinancialYear.Locked)
            _purchaseReturn.FinancialYearId = _selectedFinancialYear.Id;
        else
        {
            await _toastNotification.ShowAsync("Invalid Transaction Date", "The selected transaction date does not fall within an active financial year.", ToastType.Error);
            _purchaseReturn.TransactionDateTime = await CommonData.LoadCurrentDateTime();
            _selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_purchaseReturn.TransactionDateTime);
            _purchaseReturn.FinancialYearId = _selectedFinancialYear.Id;
        }
        #endregion

        if (_autoGenerateTransactionNo)
            _purchaseReturn.TransactionNo = await GenerateCodes.GeneratePurchaseReturnTransactionNo(_purchaseReturn);
    }

    private async Task SaveTransactionFile(bool customRoundOff = false)
    {
        if (_isProcessing || _isLoading)
            return;

        try
        {
            _isProcessing = true;

            await UpdateFinancialDetails(customRoundOff);

            await DataStorageService.LocalSaveAsync(StorageFileNames.PurchaseReturnDataFileName, System.Text.Json.JsonSerializer.Serialize(_purchaseReturn));
            await DataStorageService.LocalSaveAsync(StorageFileNames.PurchaseReturnCartDataFileName, System.Text.Json.JsonSerializer.Serialize(_cart));
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("An Error Occurred While Saving Purchase Data", ex.Message, ToastType.Error);
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
        if (_selectedCompany is null || _purchaseReturn.CompanyId <= 0)
        {
            await _toastNotification.ShowAsync("Company Not Selected", "Please select a company for the transaction.", ToastType.Error);
            return false;
        }

        if (_selectedParty is null || _purchaseReturn.PartyId <= 0)
        {
            await _toastNotification.ShowAsync("Party Not Selected", "Please select a party ledger for the transaction.", ToastType.Error);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_purchaseReturn.TransactionNo))
        {
            await _toastNotification.ShowAsync("Transaction Number Missing", "Please enter a transaction number for the transaction.", ToastType.Error);
            return false;
        }

        if (_purchaseReturn.TransactionDateTime == default)
        {
            await _toastNotification.ShowAsync("Transaction Date Missing", "Please select a valid transaction date for the transaction.", ToastType.Error);
            return false;
        }

        if (_selectedFinancialYear is null || _purchaseReturn.FinancialYearId <= 0)
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

        if (_purchaseReturn.TotalItems <= 0)
        {
            await _toastNotification.ShowAsync("Cart is Empty", "Please add at least one item to the cart before saving the transaction.", ToastType.Error);
            return false;
        }

        if (_purchaseReturn.TotalQuantity <= 0)
        {
            await _toastNotification.ShowAsync("Invalid Total Quantity", "The total quantity of items in the cart must be greater than zero.", ToastType.Error);
            return false;
        }

        if (_purchaseReturn.TotalAmount < 0)
        {
            await _toastNotification.ShowAsync("Invalid Total Amount", "The total amount of the transaction must be greater than zero.", ToastType.Error);
            return false;
        }

        if (_cart.Any(item => item.Quantity <= 0))
        {
            await _toastNotification.ShowAsync("Invalid Item Quantity", "One or more items in the cart have a quantity less than or equal to zero. Please correct the quantities before saving.", ToastType.Error);
            return false;
        }

        if (_purchaseReturn.Id > 0)
        {
            var existingPurchaseReturn = await CommonData.LoadTableDataById<PurchaseReturnModel>(TableNames.PurchaseReturn, _purchaseReturn.Id);
            var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, existingPurchaseReturn.FinancialYearId);
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

        _purchaseReturn.DocumentUrl = _purchaseReturn.DocumentUrl?.Trim();
        if (string.IsNullOrWhiteSpace(_purchaseReturn.DocumentUrl))
            _purchaseReturn.DocumentUrl = null;

        _purchaseReturn.Remarks = _purchaseReturn.Remarks?.Trim();
        if (string.IsNullOrWhiteSpace(_purchaseReturn.Remarks))
            _purchaseReturn.Remarks = null;

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

            _purchaseReturn.Status = true;
            var currentDateTime = await CommonData.LoadCurrentDateTime();
            _purchaseReturn.TransactionDateTime = DateOnly.FromDateTime(_purchaseReturn.TransactionDateTime).ToDateTime(new TimeOnly(currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second));
            _purchaseReturn.LastModifiedAt = currentDateTime;
            _purchaseReturn.CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
            _purchaseReturn.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
            _purchaseReturn.CreatedBy = _user.Id;
            _purchaseReturn.LastModifiedBy = _user.Id;

            _purchaseReturn.Id = await PurchaseReturnData.SavePurchaseReturnTransaction(_purchaseReturn, _cart);
            var (pdfStream, fileName) = await PurchaseReturnData.GenerateAndDownloadInvoice(_purchaseReturn.Id);
            await SaveAndViewService.SaveAndView(fileName, pdfStream);
            await DeleteLocalFiles();
            NavigationManager.NavigateTo(PageRouteNames.PurchaseReturn, true);

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
        await DataStorageService.LocalRemove(StorageFileNames.PurchaseReturnDataFileName);
        await DataStorageService.LocalRemove(StorageFileNames.PurchaseReturnCartDataFileName);
    }
    #endregion

    #region Uploading Document
    private void UploadDocument()
    {
        _isUploadDialogVisible = true;
        StateHasChanged();
    }

    private async Task OnRemoveFile(RemovingEventArgs args) =>
        await RemoveExistingDocument();

    private async Task RemoveExistingDocument()
    {
        try
        {
            if (string.IsNullOrEmpty(_purchaseReturn.DocumentUrl))
                return;

            var fileName = _purchaseReturn.DocumentUrl.Split('/').Last();
            await BlobStorageAccess.DeleteFileFromBlobStorage(fileName, BlobStorageContainers.purchasereturn);
            _purchaseReturn.DocumentUrl = null;

            await SaveTransactionFile();
            await _toastNotification.ShowAsync("Document Removed", "The uploaded document has been removed successfully.", ToastType.Success);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("An Error Occurred While Removing Document", ex.Message, ToastType.Error);
        }
    }

    private async Task DownloadExistingDocument()
    {
        try
        {
            if (string.IsNullOrEmpty(_purchaseReturn.DocumentUrl))
                return;

            var (fileStream, contentType) = await BlobStorageAccess.DownloadFileFromBlobStorage(_purchaseReturn.DocumentUrl, BlobStorageContainers.purchasereturn);
            var fileName = _purchaseReturn.DocumentUrl.Split('/').Last();
            await SaveAndViewService.SaveAndView(fileName, fileStream);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("An Error Occurred While Downloading Document", ex.Message, ToastType.Error);
        }
    }

    private void CloseUploadDialog()
    {
        _isUploadDialogVisible = false;
        StateHasChanged();
    }

    private async Task OnUploaderFileChange(UploadChangeEventArgs args)
    {
        try
        {
            var uploadedFiles = args.Files;

            if (uploadedFiles is not null && uploadedFiles.Count == 1)
            {
                if (!string.IsNullOrEmpty(_purchaseReturn.DocumentUrl))
                    await RemoveExistingDocument();

                await using var file = uploadedFiles[0].File.OpenReadStream(maxAllowedSize: 52428800); // 50 MB
                var fileName = $"{Guid.NewGuid()}_{uploadedFiles[0].File.Name}";
                var fileUrl = await BlobStorageAccess.UploadFileToBlobStorage(file, fileName, BlobStorageContainers.purchasereturn);
                _purchaseReturn.DocumentUrl = fileUrl; await SaveTransactionFile();
                await _toastNotification.ShowAsync("Document Uploaded Successfully", "The document has been uploaded and linked to the purchase return transaction.", ToastType.Success);
            }
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("An Error Occurred While Uploading Document", ex.Message, ToastType.Error);
        }
    }

    private async Task InterpretFiles()
    {
        try
        {
            await _toastNotification.ShowAsync("Feature Not Implemented", "The interpret files feature is not yet implemented.", ToastType.Warning);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("An Error Occurred While Interpreting Files", ex.Message, ToastType.Error);
        }
    }
    #endregion

    #region Utilities
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
            var (pdfStream, fileName) = await PurchaseReturnData.GenerateAndDownloadInvoice(Id.Value);
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

    private async Task ResetPage()
    {
        await DeleteLocalFiles();
        NavigationManager.NavigateTo(PageRouteNames.PurchaseReturn, true);
    }

    private async Task NavigateToTransactionHistoryPage()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportPurchaseReturn, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ReportPurchaseReturn);
    }

    private async Task NavigateToItemReport()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportPurchaseReturnItem, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ReportPurchaseReturnItem);
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