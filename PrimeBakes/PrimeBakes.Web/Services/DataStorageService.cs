using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;

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

		await LocalRemove(StorageFileNames.OrderCartDataFileName);
		await LocalRemove(StorageFileNames.SaleDataFileName);
		await LocalRemove(StorageFileNames.SaleCartDataFileName);
		await LocalRemove(StorageFileNames.PurchaseDataFileName);
		await LocalRemove(StorageFileNames.PurchaseCartDataFileName);
	}


	public async Task<bool> LocalExists(string key) =>
		File.Exists(key);

	public async Task LocalSaveAsync(string key, string value) =>
		await File.WriteAllTextAsync(key, value);

	public async Task<string?> LocalGetAsync(string key)
	{
		if (File.Exists(key))
			return await File.ReadAllTextAsync(key);

		return null;
	}

	public async Task LocalRemove(string key) =>
		File.Delete(key);
}
