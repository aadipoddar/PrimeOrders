using PrimeBakes.Shared.Services;

namespace PrimeBakes.Web.Services;

public class UpdateService : IUpdateService
{
	public async Task<bool> CheckForUpdatesAsync(string githubRepoOwner, string githubRepoName, string currentVersion)
	{
		await Task.CompletedTask;
		return false;
	}

	public async Task UpdateAppAsync(string githubRepoOwner, string githubRepoName, string setupAPKName, IProgress<int> progress = null)
	{
		await Task.CompletedTask;
	}
}