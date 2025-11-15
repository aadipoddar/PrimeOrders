using Android.Content;
using Android.OS;

using Java.IO;

using Application = Android.App.Application;
using Environment = Android.OS.Environment;
using Uri = Android.Net.Uri;

namespace PrimeBakes.Services;

public partial class SaveService
{
    public partial string SaveAndView(string filename, MemoryStream stream)
	{
        string root;

        if (Environment.IsExternalStorageEmulated)
			root = Application.Context!.GetExternalFilesDir(Environment.DirectoryDownloads)!.AbsolutePath;
		else
			root = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);

		Java.IO.File myDir = new(root + "/Syncfusion");
		myDir.Mkdir();

		Java.IO.File file = new(myDir, filename);

		if (file.Exists())
			file.Delete();

		try
		{
			FileOutputStream outs = new(file);
			outs.Write(stream.ToArray());

			outs.Flush();
			outs.Close();
		}
		catch (Exception ex)
        {
			return $"Error saving file: {ex}";
		}

		if (file.Exists())
		{
			string extension = Path.GetExtension(filename).ToLower();
			var mimeTypes = new Dictionary<string, string>
				{
					// Documents
					{ ".pdf", "application/pdf" },
					{ ".doc", "application/msword" },
					{ ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
					
					// Spreadsheets
					{ ".xls", "application/vnd.ms-excel" },
					{ ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
					{ ".csv", "text/csv" },
					
					// Images
					{ ".jpg", "image/jpeg" },
					{ ".jpeg", "image/jpeg" },
					{ ".png", "image/png" },
					{ ".gif", "image/gif" },
					{ ".bmp", "image/bmp" },
					{ ".svg", "image/svg+xml" },
					{ ".webp", "image/webp" },
					
					// Archives
					{ ".zip", "application/zip" },
					{ ".rar", "application/x-rar-compressed" },
					{ ".7z", "application/x-7z-compressed" },
					
					// Text
					{ ".txt", "text/plain" },
					{ ".json", "application/json" },
					{ ".xml", "application/xml" },
					
					// Other
					{ ".ppt", "application/vnd.ms-powerpoint" },
					{ ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" }
				};

            var contentType = mimeTypes.TryGetValue(extension, out string value) ? value : "application/octet-stream";

			if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
			{
				var fileUri = AndroidX.Core.Content.FileProvider.GetUriForFile(Application.Context, Application.Context.PackageName + ".provider", file);
				var intent = new Intent(Intent.ActionView);
				intent.SetData(fileUri);
				intent.AddFlags(ActivityFlags.NewTask);
				intent.AddFlags(ActivityFlags.GrantReadUriPermission);
				Application.Context.StartActivity(intent);
			}
			else
			{
				var fileUri = Uri.Parse(file.AbsolutePath);
				var intent = new Intent(Intent.ActionView);
				intent.SetDataAndType(fileUri, contentType);
				intent = Intent.CreateChooser(intent, "Open File");
				intent!.AddFlags(ActivityFlags.NewTask);
				Application.Context.StartActivity(intent);
			}

			return file.AbsolutePath;
		}

		return null;
	}
}