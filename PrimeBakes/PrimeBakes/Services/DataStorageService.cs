using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;

namespace PrimeBakes.Services;

public class DataStorageService : IDataStorageService
{
	public async Task SecureSaveAsync(string key, string value) =>
		await SecureStorage.Default.SetAsync(key, value);

	public async Task<string?> SecureGetAsync(string key) =>
		await SecureStorage.Default.GetAsync(key);

	public async Task SecureRemove(string key) =>
		SecureStorage.Default.Remove(key);

	public async Task SecureRemoveAll()
	{
		SecureStorage.Default.RemoveAll();

		await LocalRemove(StorageFileNames.UserDataFileName);
		await LocalRemove(StorageFileNames.OrderDataFileName);
		await LocalRemove(StorageFileNames.OrderCartDataFileName);
		await LocalRemove(StorageFileNames.SaleDataFileName);
		await LocalRemove(StorageFileNames.SaleCartDataFileName);
		await LocalRemove(StorageFileNames.PurchaseDataFileName);
		await LocalRemove(StorageFileNames.PurchaseCartDataFileName);
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
		File.Exists(Path.Combine(FileSystem.Current.AppDataDirectory, key));

	public async Task LocalSaveAsync(string key, string value) =>
		await File.WriteAllTextAsync(Path.Combine(FileSystem.Current.AppDataDirectory, key), value);

	public async Task<string?> LocalGetAsync(string key)
	{
		if (File.Exists(Path.Combine(FileSystem.Current.AppDataDirectory, key)))
			return await File.ReadAllTextAsync(Path.Combine(FileSystem.Current.AppDataDirectory, key));

		return null;
	}

	public async Task LocalRemove(string key) =>
		File.Delete(Path.Combine(FileSystem.Current.AppDataDirectory, key));
}
