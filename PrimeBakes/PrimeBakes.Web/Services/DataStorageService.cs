using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.DataAccess;
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
		await _protectedLocalStorage.DeleteAsync(StorageFileNames.UserDataFileName);

		await LocalRemove(StorageFileNames.OrderDataFileName);
		await LocalRemove(StorageFileNames.OrderCartDataFileName);
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


	public async Task<bool> LocalExists(string key)
	{
		await Task.CompletedTask;

		var directoryPath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			Secrets.DatabaseName);

		return File.Exists(Path.Combine(directoryPath, key));
	}

	public async Task LocalSaveAsync(string key, string value)
	{
		var directoryPath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			Secrets.DatabaseName);

		Directory.CreateDirectory(directoryPath);

		var filePath = Path.Combine(directoryPath, key);
		await File.WriteAllTextAsync(filePath, value);
	}

	public async Task<string?> LocalGetAsync(string key)
	{
		var directoryPath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			Secrets.DatabaseName);

		if (await LocalExists(key))
			return await File.ReadAllTextAsync(Path.Combine(directoryPath, key));

		return null;
	}

	public async Task LocalRemove(string key)
	{
		await Task.CompletedTask;
		var filePath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			Secrets.DatabaseName,
			key);

		if (File.Exists(filePath))
			File.Delete(filePath);
	}
}
