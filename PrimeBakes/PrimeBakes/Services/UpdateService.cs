using PrimeBakes.Shared.Services;

namespace PrimeBakes.Services;

public class UpdateService : IUpdateService
{
    public async Task<bool> CheckForUpdatesAsync(string githubRepoOwner, string githubRepoName, string currentVersion)
    {
#if ANDROID
        return await Android.AadiSoftUpdater.CheckForUpdates(githubRepoOwner, githubRepoName, currentVersion);
#else
        await Task.CompletedTask;
        // Feature will come soon for other platforms
        return false;
#endif
    }

    public async Task UpdateAppAsync(string githubRepoOwner, string githubRepoName, string setupAPKName, IProgress<int> progress = null)
    {
#if ANDROID
        await Android.AadiSoftUpdater.UpdateApp(githubRepoOwner, githubRepoName, setupAPKName, progress);
#else
        await Task.CompletedTask;
#endif
    }
}
