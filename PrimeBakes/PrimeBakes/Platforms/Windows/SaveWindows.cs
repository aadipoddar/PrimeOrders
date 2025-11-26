using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Popups;

namespace PrimeBakes.Services;

public partial class SaveService
{
    public partial string SaveAndView(string filename, MemoryStream stream)
    {
        StorageFile stFile;
        string extension = Path.GetExtension(filename).ToLower();
        //Gets process windows handle to open the dialog in application process. 
        IntPtr windowHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
        if (!Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons"))
        {
            //Creates file save picker to save a file. 
            FileSavePicker savePicker = new()
            {
                SuggestedFileName = filename
            };

            // Comprehensive file type mappings
            var fileTypeMappings = new Dictionary<string, (string description, string[] extensions)>
            {
				// Documents
				{ ".pdf", ("PDF", [".pdf"]) },
                { ".doc", ("DOC", [".doc"]) },
                { ".docx", ("DOCX", [".docx"]) },
                { ".rtf", ("RTF", [".rtf"]) },
				
				// Spreadsheets
				{ ".xls", ("XLS", [".xls"]) },
                { ".xlsx", ("XLSX", [".xlsx"]) },
                { ".csv", ("CSV", [".csv"]) },
				
				// Images
				{ ".jpg", ("JPEG", [".jpg"]) },
                { ".jpeg", ("JPEG", [".jpeg"]) },
                { ".png", ("PNG", [".png"]) },
                { ".gif", ("GIF", [".gif"]) },
                { ".bmp", ("BMP", [".bmp"]) },
                { ".svg", ("SVG", [".svg"]) },
                { ".webp", ("WEBP", [".webp"]) },
				
				// Presentations
				{ ".ppt", ("PPT", [".ppt"]) },
                { ".pptx", ("PPTX", [".pptx"]) },
				
				// Archives
				{ ".zip", ("ZIP", [".zip"]) },
                { ".rar", ("RAR", [".rar"]) },
                { ".7z", ("7Z", [".7z"]) },
				
				// Text
				{ ".txt", ("TXT", [".txt"]) },
                { ".json", ("JSON", [".json"]) },
                { ".xml", ("XML", [".xml"]) }
            };

            if (fileTypeMappings.TryGetValue(extension, out var fileType))
            {
                savePicker.DefaultFileExtension = extension;
                savePicker.FileTypeChoices.Add(fileType.description, fileType.extensions);
            }
            else
            {
                // Default fallback for unknown file types
                savePicker.DefaultFileExtension = extension;
                savePicker.FileTypeChoices.Add("All Files", [extension]);
            }

            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, windowHandle);
            stFile = savePicker.PickSaveFileAsync().GetAwaiter().GetResult();
        }
        else
        {
            StorageFolder local = ApplicationData.Current.LocalFolder;
            stFile = local.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting).GetAwaiter().GetResult();
        }

        if (stFile is not null)
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
