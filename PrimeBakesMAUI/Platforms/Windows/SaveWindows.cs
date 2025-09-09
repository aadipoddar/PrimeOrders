using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Popups;

namespace PrimeBakes.Services;

public partial class SaveService
{
	public partial string SaveAndView(string filename, string contentType, MemoryStream stream)
	{
		StorageFile stFile;
		string extension = Path.GetExtension(filename);
		//Gets process windows handle to open the dialog in application process. 
		IntPtr windowHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
		if (!Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons"))
		{
			//Creates file save picker to save a file. 
			FileSavePicker savePicker = new();
			if (extension == ".xlsx")
			{
				savePicker.DefaultFileExtension = ".xlsx";
				savePicker.SuggestedFileName = filename;
				//Saves the file as xlsx file.
				savePicker.FileTypeChoices.Add("XLSX", [".xlsx"]);
			}
			if (extension == ".docx")
			{
				savePicker.DefaultFileExtension = ".docx";
				savePicker.SuggestedFileName = filename;
				//Saves the file as Docx file.
				savePicker.FileTypeChoices.Add("DOCX", [".docx"]);
			}
			else if (extension == ".doc")
			{
				savePicker.DefaultFileExtension = ".doc";
				savePicker.SuggestedFileName = filename;
				//Saves the file as Doc file.
				savePicker.FileTypeChoices.Add("DOC", [".doc"]);
			}
			else if (extension == ".rtf")
			{
				savePicker.DefaultFileExtension = ".rtf";
				savePicker.SuggestedFileName = filename;
				//Saves the file as Rtf file.
				savePicker.FileTypeChoices.Add("RTF", [".rtf"]);
			}
			else if (extension == ".pdf")
			{
				savePicker.DefaultFileExtension = ".pdf";
				savePicker.SuggestedFileName = filename;
				//Saves the file as Pdf file.
				savePicker.FileTypeChoices.Add("PDF", [".pdf"]);
			}
			else if (extension == ".pptx")
			{
				savePicker.DefaultFileExtension = ".pptx";
				savePicker.SuggestedFileName = filename;
				//Saves the file as pptx file.
				savePicker.FileTypeChoices.Add("PPTX", [".pptx"]);
			}
			else if (extension == ".png")
			{
				savePicker.DefaultFileExtension = ".png";
				savePicker.SuggestedFileName = filename;
				//Saves the file as png file.
				savePicker.FileTypeChoices.Add("PNG", [".png"]);
			}

			WinRT.Interop.InitializeWithWindow.Initialize(savePicker, windowHandle);
			stFile = savePicker.PickSaveFileAsync().GetAwaiter().GetResult();
		}
		else
		{
			StorageFolder local = ApplicationData.Current.LocalFolder;
			stFile = local.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting).GetAwaiter().GetResult();
		}
		if (stFile != null)
		{
			using (IRandomAccessStream zipStream = stFile.OpenAsync(FileAccessMode.ReadWrite).GetAwaiter().GetResult())
			{
				//Writes compressed data from memory to file.
				using Stream outstream = zipStream.AsStreamForWrite();
				outstream.SetLength(0);
				//Saves the stream as file.
				byte[] buffer = stream.ToArray();
				outstream.Write(buffer, 0, buffer.Length);
				outstream.Flush();
			}
			//Create message dialog box. 
			MessageDialog msgDialog = new("Do you want to view the document?", "File has been created successfully");
			UICommand yesCmd = new("Yes");
			msgDialog.Commands.Add(yesCmd);
			UICommand noCmd = new("No");
			msgDialog.Commands.Add(noCmd);

			WinRT.Interop.InitializeWithWindow.Initialize(msgDialog, windowHandle);

			//Showing a dialog box. 
			IUICommand cmd = msgDialog.ShowAsync().GetAwaiter().GetResult();
			if (cmd.Label == yesCmd.Label)
			{
				//Launch the saved file. 
				Windows.System.Launcher.LaunchFileAsync(stFile).GetAwaiter().GetResult();
			}

			return stFile.Path;
		}

		return null;
	}
}
