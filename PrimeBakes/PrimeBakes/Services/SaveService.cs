using PrimeBakes.Shared.Services;

namespace PrimeBakes.Services;

public partial class SaveService
{
    //Method to save document as a file and view the saved document.
    public partial string SaveAndView(string filename, MemoryStream stream);
}

public class SaveAndViewService : ISaveAndViewService
{
    public async Task<string> SaveAndView(string filename, MemoryStream stream)
    {
        SaveService saveService = new();
        return saveService.SaveAndView(filename, stream);
    }
}
