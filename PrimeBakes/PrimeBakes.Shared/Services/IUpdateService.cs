namespace PrimeBakes.Shared.Services;

public interface IUpdateService
{
    Task<bool> CheckForUpdatesAsync(string githubRepoOwner, string githubRepoName, string currentVersion);
    Task UpdateAppAsync(string githubRepoOwner, string githubRepoName, string setupAPKName, IProgress<int> progress = null);
}
