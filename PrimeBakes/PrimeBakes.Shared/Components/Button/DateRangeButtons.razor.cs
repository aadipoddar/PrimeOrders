using Microsoft.AspNetCore.Components;

using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakes.Shared.Components.Button;

public partial class DateRangeButtons
{
	[Parameter]
	public bool Disabled { get; set; } = false;

	[Parameter]
	public EventCallback<DateRangeType> OnDateRangeSelected { get; set; }
}
