namespace PrimeBakes.Shared.Services;

public interface ISaveAndViewService
{
	public Task<string> SaveAndView(string fileName, string contentType, MemoryStream stream);
}
