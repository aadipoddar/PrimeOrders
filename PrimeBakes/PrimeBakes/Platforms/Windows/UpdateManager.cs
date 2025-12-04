using System.Diagnostics;

namespace PrimeBakes.Services.WindowsPlatform;

/// <summary>
/// Automatic Updater for Windows MAUI Applications.
/// Check for updates on the Github repository by using CheckForUpdates().
/// Update the App by using UpdateApp().
/// Downloads ZIP from Github Releases, extracts and replaces existing files.
/// </summary>
public static class AadiSoftUpdater
{
    /// <summary>
    /// Check for updates on the Github repository.
    /// It uses the Readme File in the Github Account.
    /// The readme File should have a text "Latest Version = 1.0.0.0" in it.
    /// Returns true if update is Found.
    /// </summary>
    public static async Task<bool> CheckForUpdates(string githubRepoOwner, string githubRepoName, string currentVersion)
    {
        try
        {
            var fileContent = await GetLatestVersionFromGithub(githubRepoOwner, githubRepoName);
            if (!fileContent.Contains("Latest Version = ")) return false;

            var latestVersion = fileContent.Substring(fileContent.IndexOf("Latest Version = ", StringComparison.Ordinal) + 17, 7);
            return latestVersion != currentVersion;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<string> GetLatestVersionFromGithub(string githubRepoOwner, string githubRepoName)
    {
        var fileUrl = $"https://raw.githubusercontent.com/{githubRepoOwner}/{githubRepoName}/refs/heads/main/README.md";
        using HttpClient client = new();
        var cacheBuster = DateTime.UtcNow.Ticks.ToString();
        var requestUrl = $"{fileUrl}?cb={cacheBuster}";
        return await client.GetStringAsync(requestUrl);
    }

    /// <summary>
    /// Update the App by downloading ZIP from Github Releases, extracting and replacing files.
    /// </summary>
    /// <param name="githubRepoOwner">Username of the Github Account</param>
    /// <param name="githubRepoName">Name of the Github Repository</param>
    /// <param name="zipFileName">ZIP file name (without extension)</param>
    /// <param name="progress">Optional progress reporter (0-100)</param>
    /// <param name="unused">Unused parameter for compatibility</param>
    public static async Task UpdateApp(string githubRepoOwner, string githubRepoName, string zipFileName, IProgress<int> progress = null, string unused = null)
    {
        var url = $"https://github.com/{githubRepoOwner}/{githubRepoName}/releases/latest/download/{zipFileName}.zip";
        var zipPath = Path.Combine(Path.GetTempPath(), $"{zipFileName}.zip");
        var extractPath = Path.Combine(Path.GetTempPath(), $"{zipFileName}_update");
        var appPath = AppContext.BaseDirectory;

        // Download ZIP
        using HttpClient client = new();
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? 0;
        var downloadedBytes = 0L;

        await using var stream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = new FileStream(zipPath, FileMode.Create);

        var buffer = new byte[8192];
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead);
            downloadedBytes += bytesRead;

            if (totalBytes > 0 && progress != null)
            {
                var percentage = (int)(downloadedBytes * 100 / totalBytes);
                progress.Report(percentage);
            }
        }

        fileStream.Close();

        // Run batch script to extract and replace files after app closes
        RunUpdateScript(zipPath, extractPath, appPath);
    }

    private static void RunUpdateScript(string zipPath, string extractPath, string appPath)
    {
        var batchFilePath = Path.Combine(Path.GetTempPath(), "primebakes_update.bat");
        var exePath = Path.Combine(appPath, "PrimeBakes.exe");

        var batchScript = $@"
@echo off
echo Updating Prime Bakes...
echo.
timeout /t 2 /nobreak >nul
echo Extracting update...
if exist ""{extractPath}"" rmdir /s /q ""{extractPath}""
powershell -Command ""Expand-Archive -Path '{zipPath}' -DestinationPath '{extractPath}' -Force""
echo.
echo Copying new files...
xcopy /s /y /q ""{extractPath}\*"" ""{appPath}""
echo.
echo Cleaning up...
rmdir /s /q ""{extractPath}""
del ""{zipPath}""
echo.
echo Update complete! Starting app...
start """" ""{exePath}""
del ""%~f0""
";

        File.WriteAllText(batchFilePath, batchScript);

        var startInfo = new ProcessStartInfo
        {
            FileName = batchFilePath,
            UseShellExecute = true,
            CreateNoWindow = false
        };

        Process.Start(startInfo);

        // Exit the current application to allow update
        Environment.Exit(0);
    }
}
