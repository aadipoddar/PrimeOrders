using Android.Content;

using Application = Android.App.Application;

namespace PrimeBakes.Services.Android;

public static class AadiSoftUpdater
{
	public static async Task<bool> CheckForUpdates(string githubRepoOwner, string githubRepoName, string currentVersion)
	{
		var fileContent = await GetLatestVersionFromGithub(githubRepoOwner, githubRepoName);
		if (!fileContent.Contains("Latest Version = ")) return false;
		var latestVersion = fileContent.Substring(fileContent.IndexOf("Latest Version = ", StringComparison.Ordinal) + 25, 7);
		return latestVersion != currentVersion;
	}

	private static async Task<string> GetLatestVersionFromGithub(string githubRepoOwner, string githubRepoName)
	{
		var fileUrl = $"https://raw.githubusercontent.com/{githubRepoOwner}/{githubRepoName}/refs/heads/main/README.md";
		using HttpClient client = new();
		return await client.GetStringAsync(fileUrl);
	}

	public static async Task UpdateApp(string githubRepoOwner, string githubRepoName, string setupAPKName, IProgress<int> progress = null)
	{
		var url = $"https://github.com/{githubRepoOwner}/{githubRepoName}/releases/latest/download/{setupAPKName}.apk";
		var filePath = Path.Combine(Application.Context.GetExternalFilesDir(null).AbsolutePath, $"{setupAPKName}.apk");

		using HttpClient client = new();
		using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
		response.EnsureSuccessStatusCode();

		var totalBytes = response.Content.Headers.ContentLength ?? 0;
		var downloadedBytes = 0L;

		await using var stream = await response.Content.ReadAsStreamAsync();
		await using var fileStream = new FileStream(filePath, FileMode.Create);

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

		InstallApk(filePath);
	}

	private static void InstallApk(string filePath)
	{
		var file = new Java.IO.File(filePath);
		var fileUri = AndroidX.Core.Content.FileProvider.GetUriForFile(Application.Context, Application.Context.PackageName + ".provider", file);
		var intent = new Intent(Intent.ActionView);
		intent.SetData(fileUri);
		intent.AddFlags(ActivityFlags.NewTask);
		intent.AddFlags(ActivityFlags.GrantReadUriPermission);
		Application.Context.StartActivity(intent);
	}
}