using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakes.Shared.Services;

namespace PrimeBakes.Web.Services;

public class SoundService(IJSRuntime jsRuntime) : ISoundService
{
	[Inject] private IJSRuntime JSRuntime { get; set; } = jsRuntime;

	public async Task PlaySound(string soundFileName) =>
		await JSRuntime.InvokeVoidAsync("PlaySound", soundFileName);
}
