using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakes.Shared.Services;

namespace PrimeBakes.Web.Services;

public class SaveAndViewService(IJSRuntime jsRuntime) : ISaveAndViewService
{
	[Inject] private IJSRuntime JSRuntime { get; set; } = jsRuntime;

	public async Task<string> SaveAndView(string fileName, MemoryStream stream)
	{
		await JSRuntime.InvokeVoidAsync("saveFile", Convert.ToBase64String(stream.ToArray()), fileName);
		return fileName;
	}
}
