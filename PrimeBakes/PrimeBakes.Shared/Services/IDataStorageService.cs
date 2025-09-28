namespace PrimeBakes.Shared.Services;

public interface IDataStorageService
{
	public Task SecureSaveAsync(string key, string value);
	public Task<string?> SecureGetAsync(string key);
	public Task SecureRemove(string key);
	public Task SecureRemoveAll();

	public Task<bool> LocalExists(string key);
	public Task LocalSaveAsync(string key, string value);
	public Task<string?> LocalGetAsync(string key);
	public Task LocalRemove(string key);
}
