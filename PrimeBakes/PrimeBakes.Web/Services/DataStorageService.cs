using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Models.Common;

namespace PrimeBakes.Web.Services;

public class DataStorageService(ProtectedLocalStorage protectedLocalStorage) : IDataStorageService
{
    private readonly ProtectedLocalStorage _protectedLocalStorage = protectedLocalStorage;

    public async Task SecureSaveAsync(string key, string value) =>
        await _protectedLocalStorage.SetAsync(key, value);

    public async Task<string?> SecureGetAsync(string key) =>
        (await _protectedLocalStorage.GetAsync<string>(key)).Value;

    public async Task SecureRemove(string key) =>
        await _protectedLocalStorage.DeleteAsync(key);

    public async Task SecureRemoveAll()
    {
        await LocalRemove(StorageFileNames.UserDataFileName);
        await LocalRemove(StorageFileNames.OrderDataFileName);
        await LocalRemove(StorageFileNames.OrderCartDataFileName);
        await LocalRemove(StorageFileNames.OrderMobileDataFileName);
        await LocalRemove(StorageFileNames.OrderMobileCartDataFileName);
        await LocalRemove(StorageFileNames.SaleDataFileName);
        await LocalRemove(StorageFileNames.SaleCartDataFileName);
        await LocalRemove(StorageFileNames.PurchaseDataFileName);
        await LocalRemove(StorageFileNames.PurchaseCartDataFileName);
        await LocalRemove(StorageFileNames.PurchaseReturnDataFileName);
        await LocalRemove(StorageFileNames.PurchaseReturnCartDataFileName);
        await LocalRemove(StorageFileNames.RawMaterialStockAdjustmentCartDataFileName);
        await LocalRemove(StorageFileNames.ProductStockAdjustmentCartDataFileName);
        await LocalRemove(StorageFileNames.KitchenIssueDataFileName);
        await LocalRemove(StorageFileNames.KitchenIssueCartDataFileName);
        await LocalRemove(StorageFileNames.KitchenProductionDataFileName);
        await LocalRemove(StorageFileNames.KitchenProductionCartDataFileName);
        await LocalRemove(StorageFileNames.FinancialAccountingDataFileName);
        await LocalRemove(StorageFileNames.FinancialAccountingCartDataFileName);
        await LocalRemove(StorageFileNames.SaleReturnDataFileName);
        await LocalRemove(StorageFileNames.SaleReturnCartDataFileName);
    }


    public async Task<bool> LocalExists(string key) =>
        (await _protectedLocalStorage.GetAsync<string>(key)).Success;

    public async Task LocalSaveAsync(string key, string value) =>
        await SecureSaveAsync(key, value);

    public async Task<string?> LocalGetAsync(string key) =>
        await SecureGetAsync(key);

    public async Task LocalRemove(string key) =>
        await SecureRemove(key);
}
